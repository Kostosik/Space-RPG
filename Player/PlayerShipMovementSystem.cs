
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
    private IReadOnlyList<StarSystemObject> _landingObjects =
    Array.Empty<StarSystemObject>();

    private StarSystemObject? _pendingLandingTarget;

    public event Action<StarSystemObject>? LandingCompleted;

    public bool AcceptPointerCommands { get; set; } = true;
    private const double PathEpsilon = 0.000001;
    private const double LandingApproachClearance = 28.0;
    private const double LandingRouteRefreshDistance = 8.0;
    private const double LandingArrivalTolerance = 3.0;

    private Vector2 _lastLandingTargetPosition;
    private bool _hasLastLandingTargetPosition;
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

    public bool HasActiveRoute => _path is not null;

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

        var start = new NavigationVector2(
            ship.Position.X,
            ship.Position.Y);

        var goal = new NavigationVector2(
            target.X,
            target.Y);

        NavigationPath? path = null;

        // Сначала пытаемся построить маршрут с запасом
        // пространства для выполнения поворотов.
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

            // Сглаживаем начальный поворот и повороты маршрута.
            path = NavigationPathSmoother.Smooth(
                rawPath,
                ship.Rotation,
                ShipTurnRadius,
                _obstacles,
                _pathfinder);

            if (path is not null)
                break;
        }

        if (path is null)
            return;

        // Корабль уже находится в точке назначения.
        if (path.Length <= PathEpsilon)
        {
            ClearPath();

            if (landingTarget is not null)
            {
                LandingCompleted?.Invoke(landingTarget);
            }

            return;
        }

        // Только после успешного построения заменяем маршрут.
        ClearPath();

        _path = path;
        _nextWaypointIndex = 1;
        _distanceTravelled = 0.0;

        _pendingLandingTarget = landingTarget;

        if (landingTarget is PlanetObject planet)
        {
            _lastLandingTargetPosition = new Vector2(
                (float)planet.X,
                (float)planet.Y);

            _hasLastLandingTargetPosition = true;
        }

        ship.Destination = target;
        ship.HasDestination = true;
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
            ClearPath();
            LandingCompleted?.Invoke(planet);
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
        if (newPath is null)
            return;

        if (newPath.Length <= PathEpsilon)
        {
            if (IsWithinLandingRange(ship.Position, planet))
            {
                ClearPath();
                LandingCompleted?.Invoke(planet);
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

    public void ResetForSystemTransition(
    Vector2 spawnPosition)
    {
        if (!_world.Exists(_shipEntity) ||
            !_world.Has<PlayerShipComponent>(_shipEntity))
        {
            return;
        }

        ClearPath();

        ref var ship =
            ref _world.Get<PlayerShipComponent>(_shipEntity);

        ship.Position = spawnPosition;
        ship.Rotation = 0.0f;
    }

    // Вызывается исключительно игровым временем.


    private void OnGameTimeAdvanced(
        GameTimeAdvanced advanced)
    {
        if (_disposed ||
            _path is null ||
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

        if (!float.IsFinite(ship.MaxSpeed) ||
            ship.MaxSpeed <= 0.0f)
        {
            return;
        }
        TryRefreshMovingLandingTarget(ref ship);

        if (_path is null &&
            _pendingLandingTarget is not PlanetObject)
        {
            return;
        }
        // Постоянная скорость вдоль реальной траектории.
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

            // Направление траектории.
            var directionX = dx / (float)distance;
            var directionY = dy / (float)distance;

            // Нос корабля всегда совпадает
            // с направлением его движения.
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

        // Конечная точка достигнута.
        if (_nextWaypointIndex >= points.Count)
        {
            // Корабль достиг конечной точки маршрута.
            var completedLanding = _pendingLandingTarget;

            if (completedLanding is null)
            {
                ClearPath();
                return;
            }

            // Проверяем положение планеты именно сейчас,
            // а не в момент построения старого маршрута.
            if (IsWithinLandingRange(ship.Position, completedLanding))
            {
                ClearPath();
                LandingCompleted?.Invoke(completedLanding);
            }

            // Если планета ушла дальше допустимой дистанции,
            // сохраняем приказ. Следующее обновление перестроит маршрут.
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
