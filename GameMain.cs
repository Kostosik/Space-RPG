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

namespace GameProject;

internal sealed class GameMain : EngineGame
{
    private readonly string _startupScenePath;
    private readonly IPointerInput? _pointerInput;

    private PlayerShipMovementSystem? _shipMovementSystem;
    private GameSession? _session;

    private EntityId _shipEntity;

    private Vector2[] _stars = [];

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
                Position = new Vector2(0.0f, 0.0f),
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

        // Игра начинается на паузе.
        _session.Simulation.Time.Pause();

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
                ShipInputActions.ToggleSimulation))
        {
            var gameTime = _session.Simulation.Time;

            if (gameTime.IsPaused)
            {
                    gameTime.Resume();
            }
            else
            {
                gameTime.Pause();
            }
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
        var movement = _shipMovementSystem;
        var path = movement?.CurrentPath;

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

                graphics.Submit(
                    new DrawDebugLineCommand(
                        previous,
                        next,
                        DebugColor.Green,
                        DebugRenderSpace.World));

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

        var gameTime = _session?.Simulation.Time;

        var status = gameTime?.IsPaused == true
            ? "PAUSED"
            : "RUNNING";

        var day = gameTime?.DayNumber ?? 1;

        graphics.Submit(
            new DrawDebugTextCommand(
                $"DAY: {day} | {status}",
                new Vector2(24.0f, 24.0f),
                DebugColor.White,
                2.0f,
                DebugRenderSpace.Screen));

        graphics.Submit(
            new DrawDebugTextCommand(
                $"Speed: {displayedSpeed:0.0} | {status}",
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
        if (_session is not null)
        {
            Console.WriteLine(
                $"Game session ended: {_session.SessionId}");

            _session = null;

            _shipMovementSystem?.Dispose();
            _shipMovementSystem = null;
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
}