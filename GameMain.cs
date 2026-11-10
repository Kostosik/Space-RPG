using Engine.Core.Assets;
using Engine.Core.Math;
using Engine.Core.Systems;
using Engine.Core.Time;
using Engine.ECS.Entities;
using Engine.Graphics.Commands;
using Engine.Graphics.DebugGraphics;
using Engine.Input;
using Engine.Networking.Replication;
using Engine.Runtime;
using Engine.Runtime.Scenes;
using Engine.Serialization.SaveLoad.Ecs;
using Engine.Serialization.SaveLoad.Worlds;
using GameProject.Galaxy;
using GameProject.Game;
using GameProject.Game.Scenes;
using GameProject.Player;
using GameProject.Ships;
using GameProject.Simulation;
using GameProject.World;

namespace GameProject;

internal sealed class GameMain : EngineGame
{
    private bool _isGalaxyMapOpen;
    private SceneManager<GameSceneContext>? _sceneManager;
    private string _selectedGalaxySystemId = "sol";
    private static readonly AssetPath StarterShipAssetPath =
    new("Definitions/Ships/starter_scout.json");
    private static readonly AssetPath BasicEngineAssetPath =
    new("Definitions/Equipment/basic_engine.json");

    private static readonly AssetPath BasicFuelTankAssetPath =
        new("Definitions/Equipment/basic_fuel_tank.json");
    private ShipDefinition? _starterShipDefinition;
    private readonly Dictionary<string, EquipmentDefinition>
    _equipmentDefinitions = new(StringComparer.Ordinal);
    private ShipStats? _starterShipStats;
    private string _galaxyMapMessage =
        "SELECT A STAR SYSTEM";
    private StarSystemObject? _landedObject;
    private StarSystemObject? _pendingLandingTransition;

    private const int GalaxyJumpFuelCost = 10;


    private readonly string _startupScenePath;
    private readonly IPointerInput? _pointerInput;

    private PlayerShipMovementSystem? _shipMovementSystem;
    private GameSession? _session;

    private EntityId _shipEntity;

    private Vector2[] _stars = [];
    private TurnExecutionController? _turnExecution;
    private PlanetObject? _hoveredPlanet;
    private DateTime _planetHoverStartedAt;
    private const double PlanetHoverDelaySeconds = 0.5;
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

        // Загружаем описание стартового корабля.
        _starterShipDefinition =
            Runtime.Services.Content.Load<ShipDefinition>(
                StarterShipAssetPath);

        var shipDefinition = _starterShipDefinition
            ?? throw new InvalidOperationException(
                "Starter ship definition was not loaded.");

        // Загружаем определения стартового оборудования.
        var basicEngine =
            Runtime.Services.Content.Load<EquipmentDefinition>(
                BasicEngineAssetPath);

        var basicFuelTank =
            Runtime.Services.Content.Load<EquipmentDefinition>(
                BasicFuelTankAssetPath);

        if (basicEngine.Id != "basic_engine" ||
            basicFuelTank.Id != "basic_fuel_tank")
        {
            throw new InvalidDataException(
                "Starter equipment definitions have unexpected IDs.");
        }

        _equipmentDefinitions.Add(
            basicEngine.Id,
            basicEngine);

        _equipmentDefinitions.Add(
            basicFuelTank.Id,
            basicFuelTank);

        // Пока создаём ECS-компонент с базовой скоростью корпуса.
        // Итоговые характеристики рассчитаем после создания GameSession.
        var initialMaxSpeed =
            shipDefinition.BaseStats?.MaxSpeed ?? 0.0f;

        // Создаём игровой корабль в ECS-мире.
        var ecs = Runtime.EcsWorld;

        _shipEntity = ecs.CreateEntity();

        ecs.Add(
            _shipEntity,
            new PlayerShipComponent
            {
                Position = new Vector2(-100.0f, 0.0f),
                Rotation = 0.0f,
                MaxSpeed = initialMaxSpeed
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

        if (_starterShipDefinition is null)
        {
            throw new InvalidOperationException(
                "Starter ship definition was not loaded.");
        }

        var playerShip = _session.Player.Ship;

        if (!string.Equals(
                playerShip.ShipDefinitionId,
                _starterShipDefinition.Id,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Player ship state does not match the loaded ship definition.");
        }
        // Пересчитываем характеристики с установленным оборудованием.
        var shipStats = new ShipStatsCalculator().Calculate(
            _starterShipDefinition,
            playerShip,
            _equipmentDefinitions);

        // Обновляем вместимость и полностью заправляем новый корабль.
        _session.Player.GalaxyNavigation.SetFuelCapacity(
            shipStats.FuelCapacity);

        _session.Player.GalaxyNavigation.AddFuel(
            shipStats.FuelCapacity);

        // Применяем скорость к ECS-компоненту.
        ref var shipComponent =
            ref Runtime.EcsWorld.Get<PlayerShipComponent>(_shipEntity);

        shipComponent.MaxSpeed = shipStats.MaxSpeed;

        _shipMovementSystem?.SetNavigationObstacles(
            _session.World.StarSystem.NavigationObstacles);

        _shipMovementSystem?.SetNavigationObstacles(
            _session.World.StarSystem.NavigationObstacles);

        var gameTime = _session.Simulation.Time;


        _shipMovementSystem?.SetLandingObjects(
            _session.World.StarSystem.Objects);

        if (_shipMovementSystem is not null)
        {
            _shipMovementSystem.LandingCompleted +=
                OnLandingCompleted;
        }
        // Игра начинается в режиме ожидания приказов.
        gameTime.Pause();

        // Один игровой день визуально проходит за 6 секунд.
        gameTime.SetDayDuration(
            TimeSpan.FromSeconds(4));
        // Подключаем орбиты планет к игровому времени.
        gameTime.Advanced += OnGameTimeAdvanced;
        // Подключаем движение корабля к игровому времени.
        _shipMovementSystem?.AttachGameTime(gameTime);

        // Создаём контроллер исполнения приказов.
        _turnExecution = new TurnExecutionController(
            gameTime,
            () => _shipMovementSystem?.HasActiveRoute == true);

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

        // Запрещаем управление кораблём в главном меню.
        if (_shipMovementSystem is not null)
        {
            _shipMovementSystem.AcceptPointerCommands = false;
        }

        // Создаём контекст сцен.
        var sceneContext = new GameSceneContext(
            Runtime,
            _pointerInput,

            // Вход в игровой процесс.
            enterGameplay: () =>
            {
                _shipMovementSystem?.EnablePointerCommandsAfterCurrentPress();
            },

            updateGameplay: UpdateGameplay,

            renderGameplay: RenderGameplay,

            drawLandingScreen: DrawPlanetScreen,

            takeOffFromPlanet: TakeOffFromPlanet
        );

        // Создаём менеджер сцен.
        _sceneManager =
            new SceneManager<GameSceneContext>(sceneContext);

        // Запускаем главное меню.
        _sceneManager.Start(new MainMenuScene());
    }

    private void OnGameTimeAdvanced(GameTimeAdvanced advanced)
    {
        if (_session is null)
            return;

        // Переводим интервал времени в игровые дни.
        var elapsedGameDays = advanced.Delta.Value.TotalDays;

        // Двигаем все планеты текущей звёздной системы.
        _session.World.StarSystem.AdvanceOrbit(elapsedGameDays);
    }

    protected override void OnUpdate(TimeSnapshot time)
    {
        _sceneManager?.Update(time);
    }


    private void UpdateGameplay(TimeSnapshot time)
    {
        _ = time;

        if (_session is null)
            return;

        var gameTime = _session.Simulation.Time;

        // После завершения посадки переключаем сцену,
        // но только когда игровой день полностью завершён.
        if (_pendingLandingTransition is not null &&
            gameTime.IsPaused)
        {
            _landedObject = _pendingLandingTransition;
            _pendingLandingTransition = null;

            if (_shipMovementSystem is not null)
            {
                _shipMovementSystem.AcceptPointerCommands = false;
            }

            _sceneManager?.RequestScene(
                new PlanetLandingScene());

            return;
        }

        var endTurnPressed =
            Runtime.Services.Input.IsPressed(
                ShipInputActions.EndTurn);

        var shiftHeld =
            Runtime.Services.Input.IsDown(
                ShipInputActions.Shift);

        // Во время исполнения дня разрешаем только
        // запрос остановки через Space.
        if (!gameTime.IsPaused)
        {
            if (endTurnPressed)
            {
                _turnExecution?.PressEndTurn(shiftHeld);
            }

            return;
        }

        // Ниже доступно управление только на паузе.
        if (Runtime.Services.Input.IsPressed(
                ShipInputActions.ToggleGalaxyMap))
        {
            _isGalaxyMapOpen = !_isGalaxyMapOpen;

            if (_isGalaxyMapOpen)
            {
                _selectedGalaxySystemId =
                    _session.Player.GalaxyNavigation.CurrentSystemId;

                _galaxyMapMessage = "SELECT A STAR SYSTEM";
            }

            if (_shipMovementSystem is not null)
            {
                _shipMovementSystem.AcceptPointerCommands =
                    !_isGalaxyMapOpen;
            }
        }

        if (_isGalaxyMapOpen)
        {
            HandleGalaxyMapInput();
            return;
        }

        if (endTurnPressed)
        {
            _turnExecution?.PressEndTurn(shiftHeld);
        }
    }


    private void TryGalaxyJump()
    {
        if (_session is null)
            return;

        var result = GalaxyJumpService.Execute(
            _session,
            _shipMovementSystem,
            _selectedGalaxySystemId,
            GalaxyJumpFuelCost,
            new Vector2(-100.0f, 0.0f));

        switch (result)
        {
            case GalaxyJumpResult.TargetNotFound:
                _galaxyMapMessage = "TARGET SYSTEM NOT FOUND";
                return;

            case GalaxyJumpResult.NoConnection:
                _galaxyMapMessage = "NO HYPERSPACE CONNECTION";
                return;

            case GalaxyJumpResult.NotEnoughFuel:
                _galaxyMapMessage = "NOT ENOUGH FUEL";
                return;

            case GalaxyJumpResult.JumpFailed:
                _galaxyMapMessage = "JUMP FAILED";
                return;

            case GalaxyJumpResult.Completed:
                _galaxyMapMessage = "HYPERSPACE JUMP COMPLETE";
                break;
        }

        _isGalaxyMapOpen = false;

        if (_shipMovementSystem is not null)
        {
            _shipMovementSystem.AcceptPointerCommands = true;
        }
    }

    private void OnLandingCompleted(
        StarSystemObject landingObject)
    {
        _pendingLandingTransition = landingObject;

        // Не обрываем игровой день.
        // Просим TurnExecutionController остановиться на его границе.
        _turnExecution?.RequestStopAtEndOfDay();
    }

    private void TakeOffFromPlanet()
    {
        _landedObject = null;

        _shipMovementSystem?.ResetLandingState();

        // Время остаётся на паузе.
        // Игрок сам решит, когда продолжить ход.
    }

    private void HandleGalaxyMapInput()
    {
        if (_session is null || _pointerInput is null)
            return;

        if (_pointerInput.IsPressed(InputMouseButton.Left))
        {
            var hovered = GalaxyMapRenderer.FindSystemAt(
                _session.World.Galaxy,
                _pointerInput.Position);

            if (hovered is not null)
            {
                _selectedGalaxySystemId = hovered.Id;
                _galaxyMapMessage = "SYSTEM SELECTED";
            }
        }

        if (Runtime.Services.Input.IsPressed(
                ShipInputActions.GalaxyJump))
        {
            TryGalaxyJump();
        }
    }

    private PlanetObject? GetHoveredPlanet()
    {
        if (_pointerInput is null || _session is null)
            return null;

        var mouseWorldPosition =
            Runtime.Services.Camera.ScreenToWorld(
                _pointerInput.Position);

        var starSystem = _session.World.StarSystem;

        foreach (var obj in starSystem.Objects)
        {
            if (obj is not PlanetObject planet)
                continue;

            var dx = mouseWorldPosition.X - (float)planet.X;
            var dy = mouseWorldPosition.Y - (float)planet.Y;

            var distanceSquared = dx * dx + dy * dy;
            var radius = (float)planet.VisualRadius;

            if (distanceSquared <= radius * radius)
                return planet;
        }

        return null;
    }

    private void DrawPlanetTooltip()
    {
        if (_hoveredPlanet is null)
            return;

        if ((DateTime.UtcNow - _planetHoverStartedAt).TotalSeconds
            < PlanetHoverDelaySeconds)
        {
            return;
        }

        var graphics = Runtime.Services.Graphics;

        const float panelWidth = 340.0f;
        const float panelHeight = 130.0f;

        var panelPosition = new Vector2(24.0f, 80.0f);

        graphics.Submit(
            new DrawUiRectangleCommand(
                panelPosition,
                new Vector2(panelWidth, panelHeight),
                new UiColor(20, 25, 40, 230)));

        graphics.Submit(
            new DrawDebugTextCommand(
                _hoveredPlanet.Name,
                new Vector2(38.0f, 94.0f),
                new DebugColor(100, 200, 255),
                2.0f,
                DebugRenderSpace.Screen));

        graphics.Submit(
            new DrawDebugTextCommand(
                _hoveredPlanet.Description,
                new Vector2(38.0f, 130.0f),
                DebugColor.White,
                1.3f,
                DebugRenderSpace.Screen));
    }

    private void RenderGameplay(double interpolationAlpha)
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
        var planetUnderCursor = GetHoveredPlanet();

        if (!ReferenceEquals(planetUnderCursor, _hoveredPlanet))
        {
            _hoveredPlanet = planetUnderCursor;
            _planetHoverStartedAt = DateTime.UtcNow;
        }
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
            // Рисуем орбиты всех планет.
            foreach (var planet in starSystem.Objects.OfType<PlanetObject>())
            {
                DrawWorldCircle(
                    starSystem.Star.X,
                    starSystem.Star.Y,
                    planet.OrbitRadius,
                    new DebugColor(45, 70, 105));
            }

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
        var shipVisualScale =
            _shipMovementSystem?.LandingVisualScale ?? 1.0f;
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
            22.0f * shipVisualScale);

        var rearLeft = AddScaled(
            AddScaled(
                ship.Position,
                direction,
                -14.0f * shipVisualScale),
            perpendicular,
            12.0f * shipVisualScale);

        var rearRight = AddScaled(
            AddScaled(
                ship.Position,
                direction,
                -14.0f * shipVisualScale),
            perpendicular,
            -12.0f * shipVisualScale);

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

        if (_isGalaxyMapOpen && _session is not null)
        {
            GalaxyMapRenderer.Draw(
                Runtime,
                _session.World.Galaxy,
                _session.Player.GalaxyNavigation,
                _selectedGalaxySystemId,
                _galaxyMapMessage,
                GalaxyJumpFuelCost);
        }

        DrawPlanetTooltip();

        graphics.Submit(
            new DrawDebugTextCommand(
                $"DAY: {day} | {progress:P0} | " +
                $"{(paused ? "PAUSED" : "RUNNING")} | {routeInfo}",
                new Vector2(24.0f, 24.0f),
                DebugColor.White,
                2.0f,
                DebugRenderSpace.Screen));
    }

    protected override void OnRender(double interpolationAlpha)
    {
        _sceneManager?.Render(interpolationAlpha);
    }

    private void DrawPlanetScreen()
    {
        var graphics = Runtime.Services.Graphics;

        graphics.Submit(
            new DrawUiRectangleCommand(
                new Vector2(0, 0),
                new Vector2(1280, 720),
                new UiColor(4, 10, 24, 255)));

        graphics.Submit(
            new DrawUiRectangleCommand(
                new Vector2(54, 45),
                new Vector2(1172, 630),
                new UiColor(9, 25, 43, 255)));

        graphics.Submit(
            new DrawUiRectangleCommand(
                new Vector2(78, 72),
                new Vector2(1124, 62),
                new UiColor(20, 64, 90, 255)));

        var name = _landedObject?.Name ?? "UNKNOWN";

        var objectType = _landedObject switch
        {
            PlanetObject => "PLANETARY SURFACE",
            StationObject => "SPACE STATION",
            _ => "UNKNOWN LOCATION"
        };

        var description = _landedObject switch
        {
            PlanetObject planet => planet.Description,
            StationObject station => station.Description,
            _ => "No information available."
        };

        DrawLandingText(
            "LANDING CONFIRMED",
            new Vector2(100, 90),
            new DebugColor(110, 220, 255),
            2.0f);

        DrawLandingText(
            name.ToUpperInvariant(),
            new Vector2(100, 190),
            DebugColor.White,
            3.0f);

        DrawLandingText(
            objectType,
            new Vector2(100, 250),
            new DebugColor(100, 200, 255),
            1.8f);

        DrawLandingText(
            description,
            new Vector2(100, 315),
            new DebugColor(190, 210, 225),
            1.5f);

        DrawLandingText(
            "SHIP STATUS: LANDED",
            new Vector2(100, 430),
            new DebugColor(100, 255, 170),
            1.8f);

        DrawLandingText(
            "Planetary services will be available here.",
            new Vector2(100, 475),
            new DebugColor(150, 180, 205),
            1.4f);

        // Кнопка взлёта.
        graphics.Submit(
            new DrawUiRectangleCommand(
                new Vector2(500, 590),
                new Vector2(280, 64),
                new UiColor(24, 100, 130, 255)));

        DrawLandingText(
            "TAKE OFF",
            new Vector2(585, 610),
            DebugColor.White,
            2.0f);
    }

    private void DrawLandingText(
        string text,
        Vector2 position,
        DebugColor color,
        float scale)
    {
        Runtime.Services.Graphics.Submit(
            new DrawDebugTextCommand(
                text,
                position,
                color,
                scale,
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
        if (_shipMovementSystem is not null)
        {
            _shipMovementSystem.LandingCompleted -=
                OnLandingCompleted;
        }
        _shipMovementSystem?.Dispose();
        _shipMovementSystem = null;

        if (_session is not null)
        {
            Console.WriteLine(
                $"Game session ended: {_session.SessionId}");
            _session.Simulation.Time.Advanced -= OnGameTimeAdvanced;
            _session = null;
        }

        if (_starterShipDefinition is not null)
        {
            Runtime.Services.Content.Unload<EquipmentDefinition>(
    BasicEngineAssetPath);

            Runtime.Services.Content.Unload<EquipmentDefinition>(
                BasicFuelTankAssetPath);

            _equipmentDefinitions.Clear();
            Runtime.Services.Content.Unload<ShipDefinition>(
                StarterShipAssetPath);

            _starterShipDefinition = null;
        }
        _sceneManager?.Dispose();
        _sceneManager = null;
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