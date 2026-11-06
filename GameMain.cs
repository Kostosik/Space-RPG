using Engine.Core.Math;
using Engine.Core.Systems;
using Engine.Core.Time;
using Engine.ECS.Entities;
using Engine.Graphics.Commands;
using Engine.Graphics.DebugGraphics;
using Engine.Input;
using Engine.Networking.Replication;
using Engine.Runtime;
using Engine.Serialization.SaveLoad.Ecs;
using Engine.Serialization.SaveLoad.Worlds;
using GameProject.Game;
using GameProject.Player;
using GameProject.Simulation;
using GameProject.World;

namespace GameProject;

internal sealed class GameMain : EngineGame
{
    private readonly string _startupScenePath;
    private readonly IPointerInput? _pointerInput;

    private PlayerShipMovementSystem? _shipMovementSystem;
    private GameSession? _session;

    private EntityId _shipEntity;

    private Vector2[] _stars = [];
    private TurnExecutionController? _turnExecution;
    public GameMain(
        EngineRuntime runtime,
        string startupScenePath,
        IPointerInput? pointerInput)
        : base(runtime)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            startupScenePath);

        _startupScenePath =
            Path.GetFullPath(startupScenePath);

        _pointerInput = pointerInput;
    }

    protected override void ConfigureRuntime()
    {
        if (!File.Exists(_startupScenePath))
        {
            throw new FileNotFoundException(
                "Startup scene was not found.",
                _startupScenePath);
        }

        var serializers =
            new EcsComponentSerializerRegistry();

        serializers.RegisterEngineComponents();
        serializers.RegisterNetworkComponents();

        var saveLoad =
            new WorldSaveLoadService(serializers);

        saveLoad.Load(
            _startupScenePath,
            Runtime.World);

        // Создаём игровой корабль в ECS-мире.
        var ecs = Runtime.EcsWorld;

        _shipEntity = ecs.CreateEntity();

        ecs.Add(
            _shipEntity,
            new PlayerShipComponent
            {
                Position = new Vector2(-100.0f, 0.0f),
                Rotation = 0.0f,
                MaxSpeed = 300.0f
            });

        _shipMovementSystem = new PlayerShipMovementSystem(
            ecs,
            _shipEntity,
            Runtime.Services.Input,
            _pointerInput,
            Runtime.Services.Camera);

        Runtime.Scheduler.Add(
            _shipMovementSystem,
            SystemPhase.Update);
    }

    protected override void OnInitialize()
    {
        _session = GameSession.CreateNew();
        _shipMovementSystem?.SetNavigationObstacles(
    _session.World.StarSystem.NavigationObstacles);
        var gameTime = _session.Simulation.Time;

        // Игра начинается в режиме ожидания приказов.
        gameTime.Pause();

        // Один игровой день визуально проходит за 6 секунд.
        gameTime.SetDayDuration(
            TimeSpan.FromSeconds(6));

        // Подключаем движение корабля к игровому времени.
        _shipMovementSystem?.AttachGameTime(gameTime);

        // Создаём контроллер исполнения приказов.
        _turnExecution = new TurnExecutionController(
            gameTime,
            () => _shipMovementSystem?.HasActiveRoute == true);

        // Подключаем перемещение корабля к игровому времени.
        _shipMovementSystem?.AttachGameTime(
            _session.Simulation.Time);

        Runtime.Services.Camera.Zoom = 1.0f;
        Runtime.Services.Camera.Position =
            new Vector2(0.0f, 0.0f); ;

        // Генерируем неподвижное звёздное поле.
        var random = new Random(12345);

        _stars = new Vector2[350];

        for (var i = 0; i < _stars.Length; i++)
        {
            _stars[i] = new Vector2(
                random.NextSingle() * 4000.0f - 2000.0f,
                random.NextSingle() * 4000.0f - 2000.0f);
        }

        Console.WriteLine(
            $"Game session created: {_session.SessionId}");

        Console.WriteLine(
            $"World created: {_session.World.Id}");

        Console.WriteLine(
            $"Player ship created: {_shipEntity}");
    }

    protected override void OnUpdate(TimeSnapshot time)
    {
        if (_session is null)
            return;

        if (Runtime.Services.Input.IsPressed(
                ShipInputActions.EndTurn))
        {
            var shiftHeld =
                Runtime.Services.Input.IsDown(
                    ShipInputActions.Shift);

            _turnExecution?.PressEndTurn(shiftHeld);
        }
    }

    protected override void OnRender(double interpolationAlpha)
    {
        var ecs = Runtime.EcsWorld;

        if (!ecs.Exists(_shipEntity) ||
            !ecs.Has<PlayerShipComponent>(_shipEntity))
        {
            return;
        }

        ref var ship =
            ref ecs.Get<PlayerShipComponent>(_shipEntity);

        var camera = Runtime.Services.Camera;

        // Камера следует за кораблём.
        camera.Position = ship.Position;

        // Звёздное поле.
        foreach (var star in _stars)
        {
            const float starSize = 0.8f;

            Runtime.Services.Graphics.Submit(
                new DrawDebugLineCommand(
                    new Vector2(
                        star.X - starSize,
                        star.Y),
                    new Vector2(
                        star.X + starSize,
                        star.Y),
                    DebugColor.White,
                    DebugRenderSpace.World));

            Runtime.Services.Graphics.Submit(
                new DrawDebugLineCommand(
                    new Vector2(
                        star.X,
                        star.Y - starSize),
                    new Vector2(
                        star.X,
                        star.Y + starSize),
                    DebugColor.White,
                    DebugRenderSpace.World));
        }

        // Объекты текущей звёздной системы.
        var starSystem = _session?.World.StarSystem;

        if (starSystem is not null)
        {
            // Центральная звезда.
            DrawWorldCircle(
                starSystem.Star.X,
                starSystem.Star.Y,
                starSystem.Star.VisualRadius,
                new DebugColor(255, 220, 100));

            // Планеты, астероиды и станции.
            foreach (var obj in starSystem.Objects)
            {
                var color = obj switch
                {
                    PlanetObject => new DebugColor(100, 180, 255),
                    AsteroidObject => new DebugColor(160, 160, 160),
                    StationObject => new DebugColor(100, 255, 150),
                    _ => DebugColor.White
                };

                DrawWorldCircle(
                    obj.X,
                    obj.Y,
                    obj.VisualRadius,
                    color);
            }
        }

        // Геометрия корабля.
        var direction = new Vector2(
            MathF.Cos(ship.Rotation),
            MathF.Sin(ship.Rotation));

        var perpendicular = new Vector2(
            -direction.Y,
            direction.X);

        var nose = AddScaled(
            ship.Position,
            direction,
            22.0f);

        var rearLeft = AddScaled(
            AddScaled(ship.Position, direction, -14.0f),
            perpendicular,
            12.0f);

        var rearRight = AddScaled(
            AddScaled(ship.Position, direction, -14.0f),
            perpendicular,
            -12.0f);

        var graphics = Runtime.Services.Graphics;

        // Отображаем оставшийся маршрут корабля.

        var gameTime = _session?.Simulation.Time;
        var movement = _shipMovementSystem;
        var path = movement?.CurrentPath;

        var availableDistanceToday = Math.Max(
            0.0,
            (double)ship.MaxSpeed *
            (1.0 - (gameTime?.DayProgress ?? 0.0)));

        var dailyDistanceRemaining = availableDistanceToday;

        var untraveledColor = new DebugColor(255, 165, 0);

        if (path is not null && movement is not null)
        {
            var previous = ship.Position;

            for (var i = movement.NextWaypointIndex;
                 i < path.Points.Count;
                 i++)
            {
                var point = path.Points[i];

                var next = new Vector2(
                    (float)point.X,
                    (float)point.Y);

                var dx = next.X - previous.X;
                var dy = next.Y - previous.Y;

                var segmentLength = Math.Sqrt(
                    (double)dx * dx +
                    (double)dy * dy);

                if (segmentLength <= 0.000001)
                {
                    previous = next;
                    continue;
                }

                // Часть, которую корабль сможет преодолеть
                // до окончания текущего игрового дня.
                var greenDistance = Math.Min(
                    dailyDistanceRemaining,
                    segmentLength);



                if (greenDistance > 0.0)
                {
                    var ratio =
                        greenDistance / segmentLength;

                    var split = new Vector2(
                        previous.X + dx * (float)ratio,
                        previous.Y + dy * (float)ratio);

                    graphics.Submit(
                        new DrawDebugLineCommand(
                            previous,
                            split,
                            DebugColor.Green,
                            DebugRenderSpace.World));

                    dailyDistanceRemaining -= greenDistance;

                    if (greenDistance < segmentLength)
                    {
                        graphics.Submit(
                            new DrawDebugLineCommand(
                                split,
                                next,
                                untraveledColor,
                                DebugRenderSpace.World));
                    }
                }
                else
                {
                    graphics.Submit(
                        new DrawDebugLineCommand(
                            previous,
                            next,
                            DebugColor.Yellow,
                            DebugRenderSpace.World));
                }

                previous = next;
            }
        }


        // Маркер цели автопилота.
        if (ship.HasDestination)
        {
            const float markerSize = 10.0f;
            var target = ship.Destination;

            graphics.Submit(
                new DrawDebugLineCommand(
                    new Vector2(
                        target.X - markerSize,
                        target.Y),
                    new Vector2(
                        target.X + markerSize,
                        target.Y),
                    DebugColor.Green,
                    DebugRenderSpace.World));

            graphics.Submit(
                new DrawDebugLineCommand(
                    new Vector2(
                        target.X,
                        target.Y - markerSize),
                    new Vector2(
                        target.X,
                        target.Y + markerSize),
                    DebugColor.Green,
                    DebugRenderSpace.World));
        }

        // Корпус.
        graphics.Submit(
            new DrawDebugLineCommand(
                nose,
                rearLeft,
                DebugColor.White,
                DebugRenderSpace.World));

        graphics.Submit(
            new DrawDebugLineCommand(
                rearLeft,
                rearRight,
                DebugColor.White,
                DebugRenderSpace.World));

        graphics.Submit(
            new DrawDebugLineCommand(
                rearRight,
                nose,
                DebugColor.White,
                DebugRenderSpace.World));

        // Центральная линия корпуса.
        graphics.Submit(
            new DrawDebugLineCommand(
                ship.Position,
                nose,
                DebugColor.Green,
                DebugRenderSpace.World));

        // Двигатель.
        if (Runtime.Services.Input.IsDown(
                ShipInputActions.Thrust))
        {
            var flame = AddScaled(
                ship.Position,
                direction,
                -25.0f);

            graphics.Submit(
                new DrawDebugLineCommand(
                    rearLeft,
                    flame,
                    DebugColor.Green,
                    DebugRenderSpace.World));

            graphics.Submit(
                new DrawDebugLineCommand(
                    flame,
                    rearRight,
                    DebugColor.Green,
                    DebugRenderSpace.World));
        }

        var paused =
            _session?.Simulation.Time.IsPaused ?? true;

        var displayedSpeed =
            !paused &&
            _shipMovementSystem?.CurrentPath is not null
                ? ship.MaxSpeed
                : 0.0f;

        var day = gameTime?.DayNumber ?? 1;
        var progress = gameTime?.DayProgress ?? 0.0;

        var routeInfo = "NO ROUTE";

        if (path is not null && ship.MaxSpeed > 0.0f)
        {
            var remainingRoute = Math.Max(
                0.0,
                path.Length - movement!.DistanceTravelled);

            var daysRemaining =
                remainingRoute <= dailyDistanceRemaining
                    ? 1
                    : 1 + (int)Math.Ceiling(
                        (remainingRoute - dailyDistanceRemaining) /
                        ship.MaxSpeed);

            routeInfo = $"ETA: {daysRemaining} DAY(S)";
        }

        graphics.Submit(
            new DrawDebugTextCommand(
                $"DAY: {day} | {progress:P0} | " +
                $"{(paused ? "PAUSED" : "RUNNING")} | {routeInfo}",
                new Vector2(24.0f, 24.0f),
                DebugColor.White,
                2.0f,
                DebugRenderSpace.Screen));
    }

    protected override void OnFixedUpdate(SimulationTime time)
    {
        _session?.Simulation.FixedUpdate(time);
    }

    protected override void OnShutdown()
    {
        _turnExecution?.Dispose();
        _turnExecution = null;

        _shipMovementSystem?.Dispose();
        _shipMovementSystem = null;

        if (_session is not null)
        {
            Console.WriteLine(
                $"Game session ended: {_session.SessionId}");

            _session = null;
        }
    }

    private static Vector2 AddScaled(
        Vector2 origin,
        Vector2 direction,
        float scale)
    {
        return new Vector2(
            origin.X + direction.X * scale,
            origin.Y + direction.Y * scale);
    }

    private void DrawWorldCircle(
    double centerX,
    double centerY,
    double radius,
    DebugColor color)
    {
        const int segments = 32;

        var graphics = Runtime.Services.Graphics;

        var previous = new Vector2(
            (float)(centerX + radius),
            (float)centerY);

        for (var i = 1; i <= segments; i++)
        {
            var angle = Math.PI * 2.0 * i / segments;

            var current = new Vector2(
                (float)(centerX + Math.Cos(angle) * radius),
                (float)(centerY + Math.Sin(angle) * radius));

            graphics.Submit(
                new DrawDebugLineCommand(
                    previous,
                    current,
                    color,
                    DebugRenderSpace.World));

            previous = current;
        }
    }
}