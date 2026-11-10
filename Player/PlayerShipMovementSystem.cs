
using Engine.Camera;
using Engine.Core.Math;
using Engine.Core.Systems;
using Engine.ECS.Entities;
using Engine.Input;
using GameProject.Navigation;
using GameProject.Simulation;
using GameProject.World;

namespace GameProject.Player;

public struct PlayerShipComponent
{
    public Vector2 Position;
    public float Rotation;
    public float MaxSpeed;

    public bool HasDestination;
    public Vector2 Destination;
}

public sealed class PlayerShipMovementSystem :
    IUpdateSystem,
    IDisposable
{
    private const double SystemBoundaryClearance =
    StarSystem.DefaultJumpBoundaryClearance;
    private StarSystem? _starSystem;
    private IReadOnlyList<StarSystemObject> _landingObjects =
    Array.Empty<StarSystemObject>();

    private StarSystemObject? _pendingLandingTarget;

    public event Action<StarSystemObject>? LandingCompleted;
    public event Action? SystemExitReached;
    private bool _isSystemExitRoute;
    public bool AcceptPointerCommands { get; set; } = true;
    private const double PathEpsilon = 0.000001;
    private const double LandingApproachClearance = 28.0;
    private const double LandingRouteRefreshDistance = 8.0;
    private const double LandingArrivalTolerance = 3.0;

    private Vector2 _lastLandingTargetPosition;
    private bool _hasLastLandingTargetPosition;

    private const double LandingAnimationDurationDays = 0.25;

    private bool _isLandingAnimating;
    private bool _landingAnimationCompleted;
    private StarSystemObject? _landingAnimationTarget;
    private Vector2 _landingAnimationStartPosition;
    private double _landingAnimationElapsedDays;

    // Предпочтительный радиус поворота корабля.
    private const double ShipTurnRadius = 40.0;

    // Дополнительный запас для построения траектории.
    private const double TurnPlanningClearance = 14.0;
    private IReadOnlyList<NavigationCircleObstacle> _obstacles;
    private static readonly IReadOnlyList<NavigationCircleObstacle>
        NoObstacles = Array.Empty<NavigationCircleObstacle>();

    private readonly Engine.ECS.World _world;
    private readonly EntityId _shipEntity;
    private readonly IInput _input;
    private readonly IPointerInput? _pointerInput;
    private readonly Camera2D _camera;

    private readonly NavigationPathfinder _pathfinder = new();

    private NavigationPath? _path;
    private int _nextWaypointIndex;

    private double _distanceTravelled;

    public bool HasActiveRoute =>
        _path is not null || _isLandingAnimating;

    public bool IsLandingAnimating => _isLandingAnimating;

    public float LandingVisualScale
    {
        get
        {
            if (!_isLandingAnimating)
                return 1.0f;

            var progress = Math.Clamp(
                _landingAnimationElapsedDays /
                LandingAnimationDurationDays,
                0.0,
                1.0);

            return (float)(1.0 - progress);
        }
    }

    public double DistanceTravelled => _distanceTravelled;

    private GameTime? _gameTime;
    private bool _disposed;
    private bool _ignoreCurrentLeftPress;
    public PlayerShipMovementSystem(
        Engine.ECS.World world,
        EntityId shipEntity,
        IInput input,
        IPointerInput? pointerInput,
        Camera2D camera,
        IReadOnlyList<NavigationCircleObstacle>? obstacles = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(camera);

        _world = world;
        _shipEntity = shipEntity;
        _input = input;
        _pointerInput = pointerInput;
        _camera = camera;
        _obstacles = obstacles is null
            ? NoObstacles
            : Array.AsReadOnly(obstacles.ToArray());
    }

    public NavigationPath? CurrentPath => _path;

    public void SetMaxSpeed(float maxSpeed)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(
                nameof(PlayerShipMovementSystem));
        }

        if (!float.IsFinite(maxSpeed) || maxSpeed <= 0.0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxSpeed));
        }

        if (!_world.Exists(_shipEntity) ||
            !_world.Has<PlayerShipComponent>(_shipEntity))
        {
            throw new InvalidOperationException(
                "Player ship entity does not exist.");
        }

        ref var ship =
            ref _world.Get<PlayerShipComponent>(_shipEntity);

        ship.MaxSpeed = maxSpeed;
    }

    public int NextWaypointIndex => _nextWaypointIndex;

    public void AttachGameTime(GameTime gameTime)
    {
        ArgumentNullException.ThrowIfNull(gameTime);

        if (_disposed)
        {
            throw new ObjectDisposedException(
                nameof(PlayerShipMovementSystem));
        }

        if (_gameTime is not null)
        {
            _gameTime.Advanced -= OnGameTimeAdvanced;
        }

        _gameTime = gameTime;
        _gameTime.Advanced += OnGameTimeAdvanced;
    }

    // Здесь обрабатываем только команды игрока.
    // Само движение выполняется через GameTime.Advanced.
    public void Update(SystemContext context)
    {
        if (_disposed)
            return;

        _ = context;

        if (!_world.Exists(_shipEntity) ||
            !_world.Has<PlayerShipComponent>(_shipEntity))
        {
            ClearPath();
            return;
        }

        TrySetDestination();
    }

    public void SetLandingObjects(
        IReadOnlyList<StarSystemObject> objects)
    {
        ArgumentNullException.ThrowIfNull(objects);

        if (objects.Any(obj => obj is null))
        {
            throw new ArgumentException(
                "Landing objects cannot contain null values.",
                nameof(objects));
        }

        _landingObjects =
            Array.AsReadOnly(objects.ToArray());

        ClearPath();
    }

    public void EnablePointerCommandsAfterCurrentPress()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(
                nameof(PlayerShipMovementSystem));
        }

        // Не позволяем клику по кнопке меню
        // стать командой движения корабля.
        _ignoreCurrentLeftPress =
            _pointerInput?.IsDown(InputMouseButton.Left) == true;

        AcceptPointerCommands = true;
    }

    public void SetNavigationObstacles(
        IReadOnlyList<NavigationCircleObstacle> obstacles)
    {
        ArgumentNullException.ThrowIfNull(obstacles);

        if (obstacles.Any(obstacle => obstacle is null))
        {
            throw new ArgumentException(
                "Navigation obstacles cannot contain null values.",
                nameof(obstacles));
        }

        // Старый маршрут больше не гарантирует безопасность.
        ClearPath();

        _obstacles =
            Array.AsReadOnly(obstacles.ToArray());
    }

    private void TrySetDestination()
    {
        if (_ignoreCurrentLeftPress)
        {
            // Пока кнопка мыши удерживается — игнорируем её.
            if (_pointerInput?.IsDown(InputMouseButton.Left) == true)
            {
                return;
            }

            // Кнопку отпустили. Следующее нажатие уже можно обработать.
            _ignoreCurrentLeftPress = false;
        }
        if (!AcceptPointerCommands ||
            _pointerInput is null ||
            !_pointerInput.IsPressed(InputMouseButton.Left))
        {
            return;
        }

        if (_gameTime is not null && !_gameTime.IsPaused)
            return;

        if (!_world.Exists(_shipEntity) ||
            !_world.Has<PlayerShipComponent>(_shipEntity))
        {
            return;
        }

        ref var ship =
            ref _world.Get<PlayerShipComponent>(_shipEntity);

        var target = _camera.ScreenToWorld(
            _pointerInput.Position);

        if (!float.IsFinite(target.X) ||
            !float.IsFinite(target.Y))
        {
            return;
        }

        var landingTarget = FindLandingTarget(target);

        if (landingTarget is not null)
        {
            target = GetLandingApproachPoint(
                landingTarget,
                ship.Position);
        }
        else if (_starSystem is not null)
        {
            var boundedTarget = _starSystem.ClampToJumpBoundary(
                new NavigationVector2(target.X, target.Y),
                SystemBoundaryClearance);

            target = new Vector2(
                (float)boundedTarget.X,
                (float)boundedTarget.Y);
        }

        TryBuildAndSetRoute(
            target,
            landingTarget,
            ref ship,
            isSystemExitRoute: false);
    }

    private bool IsPathInsideJumpBoundary(NavigationPath path)
    {
        if (_starSystem is null)
            return true;

        foreach (var point in path.Points)
        {
            if (!_starSystem.IsWithinJumpBoundary(
                    point,
                    SystemBoundaryClearance))
            {
                return false;
            }
        }

        return true;
    }

    private bool TryBuildAndSetRoute(
    Vector2 target,
    StarSystemObject? landingTarget,
    ref PlayerShipComponent ship,
    bool isSystemExitRoute)
    {
        var start = new NavigationVector2(
            ship.Position.X,
            ship.Position.Y);

        var goal = new NavigationVector2(
            target.X,
            target.Y);

        NavigationPath? path = null;

        double[] clearances =
        [
            TurnPlanningClearance,
        TurnPlanningClearance * 0.5,
        0.0
        ];

        foreach (var clearance in clearances)
        {
            var planningObstacles =
                new NavigationCircleObstacle[_obstacles.Count];

            for (var i = 0; i < _obstacles.Count; i++)
            {
                var obstacle = _obstacles[i];

                planningObstacles[i] =
                    new NavigationCircleObstacle(
                        obstacle.Center,
                        obstacle.Radius + clearance);
            }

            var rawPath = _pathfinder.FindPath(
                start,
                goal,
                planningObstacles);

            if (rawPath is null)
                continue;

            var candidate = NavigationPathSmoother.Smooth(
                rawPath,
                ship.Rotation,
                ShipTurnRadius,
                _obstacles,
                _pathfinder);

            if (candidate is null ||
                !IsPathInsideJumpBoundary(candidate))
            {
                continue;
            }

            path = candidate;
            break;
        }

        if (path is null)
            return false;

        if (path.Length <= PathEpsilon)
        {
            if (landingTarget is not null)
            {
                BeginLandingAnimation(landingTarget, ref ship);
            }
            else
            {
                ClearPath();

                if (isSystemExitRoute)
                    SystemExitReached?.Invoke();
            }

            return true;
        }

        ClearPath();

        _path = path;
        _nextWaypointIndex = 1;
        _distanceTravelled = 0.0;

        _pendingLandingTarget = landingTarget;
        _isSystemExitRoute = isSystemExitRoute;

        if (landingTarget is PlanetObject planet)
        {
            _lastLandingTargetPosition = new Vector2(
                (float)planet.X,
                (float)planet.Y);

            _hasLastLandingTargetPosition = true;
        }

        ship.Destination = target;
        ship.HasDestination = true;

        return true;
    }

    private StarSystemObject? FindLandingTarget(
    Vector2 worldPosition)
    {
        StarSystemObject? closest = null;
        var bestDistanceSquared = double.MaxValue;

        foreach (var obj in _landingObjects)
        {
            if (obj is not PlanetObject &&
                obj is not StationObject)
            {
                continue;
            }

            var dx = worldPosition.X - obj.X;
            var dy = worldPosition.Y - obj.Y;

            var distanceSquared =
                dx * dx + dy * dy;

            // Небольшая зона выбора вокруг объекта.
            var selectionRadius = obj.VisualRadius + 18.0;
            var selectionRadiusSquared =
                selectionRadius * selectionRadius;

            if (distanceSquared > selectionRadiusSquared)
                continue;

            if (distanceSquared < bestDistanceSquared)
            {
                bestDistanceSquared = distanceSquared;
                closest = obj;
            }
        }

        return closest;
    }

    private static Vector2 GetLandingApproachPoint(
    StarSystemObject target,
    Vector2 shipPosition)
    {
        var dx = shipPosition.X - target.X;
        var dy = shipPosition.Y - target.Y;

        var distance = Math.Sqrt(
            (double)dx * dx +
            (double)dy * dy);

        if (distance < 0.000001)
        {
            dx = 1.0f;
            dy = 0.0f;
            distance = 1.0;
        }

        var approachDistance =
            target.VisualRadius + LandingApproachClearance;

        return new Vector2(
            (float)(target.X + dx / distance * approachDistance),
            (float)(target.Y + dy / distance * approachDistance));
    }

    private void BeginLandingAnimation(
    StarSystemObject target,
    ref PlayerShipComponent ship)
    {
        ClearPath();

        _isLandingAnimating = true;
        _landingAnimationCompleted = false;
        _landingAnimationTarget = target;
        _landingAnimationStartPosition = ship.Position;
        _landingAnimationElapsedDays = 0.0;

        ship.HasDestination = false;

        // Во время посадки новые команды недоступны.
        AcceptPointerCommands = false;
    }

    private void AdvanceLandingAnimation(
        double elapsedGameDays,
        ref PlayerShipComponent ship)
    {
        if (!_isLandingAnimating ||
            _landingAnimationCompleted ||
            _landingAnimationTarget is null)
        {
            return;
        }

        _landingAnimationElapsedDays += elapsedGameDays;

        var progress = Math.Clamp(
            _landingAnimationElapsedDays /
            LandingAnimationDurationDays,
            0.0,
            1.0);

        // Плавное замедление при приближении к поверхности.
        var easedProgress =
            progress * progress * (3.0 - 2.0 * progress);

        var targetPosition = new Vector2(
            (float)_landingAnimationTarget.X,
            (float)_landingAnimationTarget.Y);

        ship.Position = new Vector2(
            _landingAnimationStartPosition.X +
            (targetPosition.X - _landingAnimationStartPosition.X) *
            (float)easedProgress,

            _landingAnimationStartPosition.Y +
            (targetPosition.Y - _landingAnimationStartPosition.Y) *
            (float)easedProgress);

        var direction = targetPosition - ship.Position;

        if (direction.LengthSquared() > 0.000001f)
        {
            ship.Rotation = MathF.Atan2(
                direction.Y,
                direction.X);
        }

        if (progress < 1.0)
            return;

        ship.Position = targetPosition;
        _landingAnimationCompleted = true;

        // Событие вызывается только после завершения анимации.
        LandingCompleted?.Invoke(_landingAnimationTarget);
    }

    public void ResetLandingState()
    {
        ClearPath();
    }

    private static bool IsWithinLandingRange(
        Vector2 shipPosition,
        StarSystemObject target)
    {
        var dx = shipPosition.X - target.X;
        var dy = shipPosition.Y - target.Y;

        var distanceSquared =
            (double)dx * dx + (double)dy * dy;

        var landingRadius =
            target.VisualRadius +
            LandingApproachClearance +
            LandingArrivalTolerance;

        return distanceSquared <= landingRadius * landingRadius;
    }

    public void SetStarSystem(StarSystem starSystem)
    {
        ArgumentNullException.ThrowIfNull(starSystem);

        if (!ReferenceEquals(_starSystem, starSystem))
            ClearPath();

        _starSystem = starSystem;

        _landingObjects = starSystem.Objects;
        _obstacles = starSystem.NavigationObstacles;
    }

    private void TryRefreshMovingLandingTarget(
        ref PlayerShipComponent ship)
    {
        if (_pendingLandingTarget is not PlanetObject planet)
            return;

        var currentPlanetPosition = new Vector2(
            (float)planet.X,
            (float)planet.Y);

        if (!_hasLastLandingTargetPosition)
        {
            _lastLandingTargetPosition = currentPlanetPosition;
            _hasLastLandingTargetPosition = true;
            return;
        }

        var dx =
            currentPlanetPosition.X - _lastLandingTargetPosition.X;

        var dy =
            currentPlanetPosition.Y - _lastLandingTargetPosition.Y;

        var movementSquared =
            (double)dx * dx + (double)dy * dy;

        var refreshDistanceSquared =
            LandingRouteRefreshDistance *
            LandingRouteRefreshDistance;

        if (_path is not null &&
            movementSquared < refreshDistanceSquared)
        {
            return;
        }

        // Запоминаем положение, чтобы не запускать
        // Pathfinder на каждом кадре при неудачном поиске.
        _lastLandingTargetPosition = currentPlanetPosition;

        // Планета сама подошла достаточно близко к кораблю.
        if (IsWithinLandingRange(ship.Position, planet))
        {
            BeginLandingAnimation(planet, ref ship);
            return;
        }

        var approachPoint = GetLandingApproachPoint(
            planet,
            ship.Position);

        var start = new NavigationVector2(
            ship.Position.X,
            ship.Position.Y);

        var goal = new NavigationVector2(
            approachPoint.X,
            approachPoint.Y);

        var newPath = _pathfinder.FindPath(
            start,
            goal,
            _obstacles);

        // Не заменяем рабочий маршрут, если новый построить нельзя.
        if (newPath is null ||
            !IsPathInsideJumpBoundary(newPath))
        {
            return;
        }

        if (newPath.Length <= PathEpsilon)
        {
            if (IsWithinLandingRange(ship.Position, planet))
            {
                BeginLandingAnimation(planet, ref ship);
            }

            return;
        }

        // Перестраиваем маршрут от фактической позиции корабля.
        _path = newPath;
        _nextWaypointIndex = 1;
        _distanceTravelled = 0.0;

        ship.Destination = approachPoint;
        ship.HasDestination = true;
    }

    public bool TrySetSystemExitRoute(Vector2 target)
    {
        if (_disposed ||
            _starSystem is null ||
            (_gameTime is not null && !_gameTime.IsPaused) ||
            _isLandingAnimating ||
            !_world.Exists(_shipEntity) ||
            !_world.Has<PlayerShipComponent>(_shipEntity) ||
            !float.IsFinite(target.X) ||
            !float.IsFinite(target.Y))
        {
            return false;
        }

        var destination = new NavigationVector2(
            target.X,
            target.Y);

        if (!_starSystem.IsWithinJumpBoundary(
                destination,
                SystemBoundaryClearance))
        {
            return false;
        }

        ref var ship =
            ref _world.Get<PlayerShipComponent>(_shipEntity);

        return TryBuildAndSetRoute(
            target,
            null,
            ref ship,
            isSystemExitRoute: true);
    }

    public void ResetForSystemTransition(
    Vector2 spawnPosition,
    float rotation = 0.0f)
    {
        if (!float.IsFinite(spawnPosition.X) ||
            !float.IsFinite(spawnPosition.Y) ||
            !float.IsFinite(rotation))
        {
            throw new ArgumentException(
                "Spawn position and rotation must be finite.");
        }

        if (!_world.Exists(_shipEntity) ||
            !_world.Has<PlayerShipComponent>(_shipEntity))
        {
            return;
        }

        ClearPath();

        ref var ship =
            ref _world.Get<PlayerShipComponent>(_shipEntity);

        ship.Position = spawnPosition;
        ship.Rotation = rotation;
    }

    // Вызывается исключительно игровым временем.


    private void OnGameTimeAdvanced(
    GameTimeAdvanced advanced)
    {
        if (_disposed ||
            (!_isLandingAnimating &&
             _path is null &&
             _pendingLandingTarget is not PlanetObject) ||
            !_world.Exists(_shipEntity) ||
            !_world.Has<PlayerShipComponent>(_shipEntity))
        {
            return;
        }

        var dayFraction =
            (double)advanced.Delta.Value.Ticks /
            TimeSpan.TicksPerDay;

        if (!double.IsFinite(dayFraction) ||
            dayFraction <= 0.0)
        {
            return;
        }

        ref var ship =
            ref _world.Get<PlayerShipComponent>(_shipEntity);

        // Во время посадки корабль уже не выполняет маршрут.
        if (_isLandingAnimating)
        {
            AdvanceLandingAnimation(dayFraction, ref ship);
            return;
        }

        if (!float.IsFinite(ship.MaxSpeed) ||
            ship.MaxSpeed <= 0.0f)
        {
            return;
        }

        TryRefreshMovingLandingTarget(ref ship);

        // Если только что началась посадка,
        // анимация продолжится со следующего обновления времени.
        if (_isLandingAnimating)
            return;

        if (_path is null)
            return;

        var remainingDistance =
            (double)ship.MaxSpeed * dayFraction;

        var points = _path.Points;

        while (_nextWaypointIndex < points.Count &&
               remainingDistance > PathEpsilon)
        {
            var waypoint = points[_nextWaypointIndex];

            var target = new Vector2(
                (float)waypoint.X,
                (float)waypoint.Y);

            var dx = target.X - ship.Position.X;
            var dy = target.Y - ship.Position.Y;

            var distance = Math.Sqrt(
                (double)dx * dx +
                (double)dy * dy);

            if (distance <= PathEpsilon)
            {
                ship.Position = target;
                _nextWaypointIndex++;
                continue;
            }

            var directionX = dx / (float)distance;
            var directionY = dy / (float)distance;

            ship.Rotation = MathF.Atan2(
                directionY,
                directionX);

            var travelDistance = Math.Min(
                remainingDistance,
                distance);

            var fraction = travelDistance / distance;

            ship.Position = new Vector2(
                ship.Position.X + dx * (float)fraction,
                ship.Position.Y + dy * (float)fraction);

            _distanceTravelled += travelDistance;
            remainingDistance -= travelDistance;

            if (travelDistance < distance - PathEpsilon)
                break;

            ship.Position = target;
            _nextWaypointIndex++;
        }

        if (_nextWaypointIndex < points.Count)
            return;

        var completedLanding = _pendingLandingTarget;

        if (completedLanding is null)
        {
            var completedSystemExit = _isSystemExitRoute;

            ClearPath();

            if (completedSystemExit)
                SystemExitReached?.Invoke();

            return;
        }

        if (IsWithinLandingRange(ship.Position, completedLanding))
        {
            BeginLandingAnimation(completedLanding, ref ship);
            return;
        }

        // Планета успела уйти дальше допустимой дистанции.
        // Сохраняем приказ и ждём обновления маршрута.
        if (completedLanding is PlanetObject)
        {
            _path = null;
            _nextWaypointIndex = 0;
            _distanceTravelled = 0.0;
            ship.HasDestination = false;
        }
    }



    private static float MoveTowardsAngle(
        float current,
        float target,
        float maxDelta)
    {
        if (maxDelta <= 0.0f)
            return current;

        var delta = NormalizeAngle(target - current);

        if (MathF.Abs(delta) <= maxDelta)
            return current + delta;

        return current +
               MathF.CopySign(maxDelta, delta);
    }

    private static float NormalizeAngle(float angle)
    {
        const float twoPi = MathF.PI * 2.0f;

        while (angle > MathF.PI)
            angle -= twoPi;

        while (angle < -MathF.PI)
            angle += twoPi;

        return angle;
    }

    private void ClearPath()
    {
        _path = null;
        _nextWaypointIndex = 0;
        _distanceTravelled = 0.0;
        _pendingLandingTarget = null;
        if (_world.Exists(_shipEntity) &&
            _world.Has<PlayerShipComponent>(_shipEntity))
        {
            ref var ship =
                ref _world.Get<PlayerShipComponent>(_shipEntity);

            ship.HasDestination = false;
        }
        _hasLastLandingTargetPosition = false;
        _lastLandingTargetPosition = default;
        _isLandingAnimating = false;
        _landingAnimationCompleted = false;
        _landingAnimationTarget = null;
        _landingAnimationStartPosition = default;
        _isSystemExitRoute = false;
        _landingAnimationElapsedDays = 0.0;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        if (_gameTime is not null)
        {
            _gameTime.Advanced -= OnGameTimeAdvanced;
            _gameTime = null;
        }

        ClearPath();
    }
}
