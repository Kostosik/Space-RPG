
using Engine.Camera;
using Engine.Core.Math;
using Engine.Core.Systems;
using Engine.ECS.Entities;
using Engine.Input;
using GameProject.Navigation;
using GameProject.Simulation;

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
    private const double PathEpsilon = 0.000001;

    private static readonly IReadOnlyList<NavigationCircleObstacle>
        NoObstacles = Array.Empty<NavigationCircleObstacle>();

    private readonly Engine.ECS.World _world;
    private readonly EntityId _shipEntity;
    private readonly IInput _input;
    private readonly IPointerInput? _pointerInput;
    private readonly Camera2D _camera;

    private readonly NavigationPathfinder _pathfinder = new();
    private readonly IReadOnlyList<NavigationCircleObstacle> _obstacles;

    private NavigationPath? _path;
    private int _nextWaypointIndex;

    private GameTime? _gameTime;
    private bool _disposed;

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

        _obstacles = obstacles ?? NoObstacles;
    }

    public NavigationPath? CurrentPath => _path;

    public int NextWaypointIndex => _nextWaypointIndex;

    public void AttachGameTime(GameTime gameTime)
    {
        ArgumentNullException.ThrowIfNull(gameTime);

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

    private void TrySetDestination()
    {
        if (_pointerInput is null ||
            !_pointerInput.IsPressed(InputMouseButton.Left))
        {
            return;
        }

        ref var ship =
            ref _world.Get<PlayerShipComponent>(_shipEntity);

        // Новый клик полностью заменяет предыдущий маршрут.
        ClearPath();

        // Переводим позицию мыши в мировые координаты.
        var target = _camera.ScreenToWorld(
            _pointerInput.Position);

        if (!float.IsFinite(target.X) ||
            !float.IsFinite(target.Y))
        {
            return;
        }

        var start = new NavigationVector2(
            ship.Position.X,
            ship.Position.Y);

        var goal = new NavigationVector2(
            target.X,
            target.Y);

        // Автоматически строим маршрут с обходом препятствий.
        var path = _pathfinder.FindPath(
            start,
            goal,
            _obstacles);

        if (path is null || path.Length <= PathEpsilon)
            return;

        _path = path;
        _nextWaypointIndex = 1;

        ship.Destination = target;
        ship.HasDestination = true;
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

        var deltaSeconds =
            (float)advanced.Delta.Value.TotalSeconds;

        if (!float.IsFinite(deltaSeconds) ||
            deltaSeconds <= 0.0f)
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

        // За игровой интервал корабль должен пройти
        // ровно Speed * DeltaDistance.
        var remainingDistance =
            (double)ship.MaxSpeed * deltaSeconds;

        var points = _path.Points;

        while (_nextWaypointIndex < points.Count)
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

            // Точка уже достигнута.
            if (distance <= PathEpsilon)
            {
                ship.Position = target;
                _nextWaypointIndex++;
                continue;
            }

            // Направление движения по текущему сегменту.
            var directionX = dx / (float)distance;
            var directionY = dy / (float)distance;

            // Поворачиваем изображение корабля
            // вдоль текущего сегмента маршрута.
            ship.Rotation = MathF.Atan2(
                directionY,
                directionX);

            // До следующей точки осталось меньше,
            // чем можем пройти за этот игровой интервал.
            if (remainingDistance < distance)
            {
                ship.Position = new Vector2(
                    ship.Position.X +
                    directionX * (float)remainingDistance,

                    ship.Position.Y +
                    directionY * (float)remainingDistance);

                return;
            }

            // Полностью проходим текущий сегмент.
            ship.Position = target;

            remainingDistance -= distance;
            _nextWaypointIndex++;
        }
        // Корабль достиг конечной точки.
        ClearPath();

        // Завершаем игровые сутки.
        _gameTime?.CompleteDay();
    }

    private void ClearPath()
    {
        _path = null;
        _nextWaypointIndex = 0;

        if (_world.Exists(_shipEntity) &&
            _world.Has<PlayerShipComponent>(_shipEntity))
        {
            ref var ship =
                ref _world.Get<PlayerShipComponent>(_shipEntity);

            ship.HasDestination = false;
        }
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
