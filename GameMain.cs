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
using Engine.Serialization.SaveLoad.Ecs;
using Engine.Serialization.SaveLoad.Worlds;
using GameProject.Game;
using GameProject.Player;
using GameProject.Ships;
using GameProject.Simulation;
using GameProject.World;

namespace GameProject;

internal sealed class GameMain : EngineGame
{
    private bool _isGalaxyMapOpen;

    private string _selectedGalaxySystemId = "sol";
    private static readonly AssetPath StarterShipAssetPath =
    new("Definitions/Ships/starter_scout.json");

    private ShipDefinition? _starterShipDefinition;
    private ShipStats? _starterShipStats;
    private string _galaxyMapMessage =
        "SELECT A STAR SYSTEM";
    private bool _isLanded;
    private StarSystemObject? _landedObject;
    private const float GalaxyMapCenterX = 600.0f;
    private const float GalaxyMapCenterY = 360.0f;
    private const float GalaxyMapScale = 0.65f;

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

        _starterShipStats = new ShipStatsCalculator()
            .Calculate(shipDefinition);

        if (!float.IsFinite(_starterShipStats.MaxSpeed) ||
            _starterShipStats.MaxSpeed <= 0.0f ||
            _starterShipStats.FuelCapacity <= 0)
        {
            throw new InvalidDataException(
                "Starter ship contains invalid statistics.");
        }

        // Создаём игровой корабль в ECS-мире.
        var ecs = Runtime.EcsWorld;

        _shipEntity = ecs.CreateEntity();

        ecs.Add(
            _shipEntity,
            new PlayerShipComponent
            {
                Position = new Vector2(-100.0f, 0.0f),
                Rotation = 0.0f,
                MaxSpeed = _starterShipStats.MaxSpeed
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

        _session.Player.GalaxyNavigation.SetFuelCapacity(
            _starterShipStats?.FuelCapacity
                ?? throw new InvalidOperationException(
                    "Starter ship statistics were not initialized."));

        if (_starterShipDefinition is null ||
    !string.Equals(
        _session.Player.Ship.ShipDefinitionId,
        _starterShipDefinition.Id,
        StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Player ship state does not match the loaded ship definition.");
        }

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
            TimeSpan.FromSeconds(6));
        // Подключаем орбиты планет к игровому времени.
        gameTime.Advanced += OnGameTimeAdvanced;
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
        if (_session is null)
            return;
        if (_isLanded)
        {
            HandlePlanetScreenInput();
            return;
        }
        if (Runtime.Services.Input.IsPressed(
        ShipInputActions.ToggleGalaxyMap))
        {
            _isGalaxyMapOpen = !_isGalaxyMapOpen;

            _shipMovementSystem?.GetType();

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

        if (Runtime.Services.Input.IsPressed(
                ShipInputActions.EndTurn))
        {
            var shiftHeld =
                Runtime.Services.Input.IsDown(
                    ShipInputActions.Shift);

            _turnExecution?.PressEndTurn(shiftHeld);
        }
    }

    private void TryGalaxyJump()
    {
        if (_session is null)
            return;

        var galaxy = _session.World.Galaxy;

        var navigation =
            _session.Player.GalaxyNavigation;

        var target = galaxy.GetSystem(
            _selectedGalaxySystemId);

        if (target is null)
            return;

        if (!galaxy.CanTravel(
                navigation.CurrentSystemId,
                target.Id))
        {
            _galaxyMapMessage =
                "NO HYPERSPACE CONNECTION";

            return;
        }

        if (navigation.Fuel < GalaxyJumpFuelCost)
        {
            _galaxyMapMessage =
                "NOT ENOUGH FUEL";

            return;
        }

        if (!navigation.TryJump(
                galaxy,
                target.Id,
                GalaxyJumpFuelCost))
        {
            _galaxyMapMessage =
                "JUMP FAILED";

            return;
        }

        // Загружаем новую звёздную систему.
        _session.World.EnterSystem(target.Id);
        _shipMovementSystem?.SetLandingObjects(
    _session.World.StarSystem.Objects);
        // Обновляем препятствия для навигации.
        _shipMovementSystem?.SetNavigationObstacles(
            _session.World.StarSystem.NavigationObstacles);

        // Перемещаем корабль в новую систему.
        _shipMovementSystem?.ResetForSystemTransition(
            new Vector2(-100.0f, 0.0f));

        _galaxyMapMessage = "HYPERSPACE JUMP COMPLETE";

        _isGalaxyMapOpen = false;

        if (_shipMovementSystem is not null)
        {
            _shipMovementSystem.AcceptPointerCommands = true;
        }
    }

    private void OnLandingCompleted(
    StarSystemObject landingObject)
    {
        _landedObject = landingObject;
        _isLanded = true;

        if (_shipMovementSystem is not null)
        {
            _shipMovementSystem.AcceptPointerCommands = false;
        }

        // Останавливаем игровое время после посадки.
        _session?.Simulation.Time.Pause();
    }

    private void HandlePlanetScreenInput()
    {
        if (_pointerInput is null ||
            !_pointerInput.IsPressed(InputMouseButton.Left))
        {
            return;
        }

        var position = _pointerInput.Position;

        // Кнопка взлёта.
        if (position.X >= 500.0f &&
            position.X <= 780.0f &&
            position.Y >= 590.0f &&
            position.Y <= 654.0f)
        {
            TakeOffFromPlanet();
        }
    }

    private void TakeOffFromPlanet()
    {
        _isLanded = false;
        _landedObject = null;

        if (_shipMovementSystem is not null)
        {
            _shipMovementSystem.AcceptPointerCommands = true;
        }

        // Время оставляем на паузе.
        // Игрок сам решит, когда продолжить ход.
    }

    private void DrawGalaxyMap()
    {
        if (_session is null)
            return;

        var graphics = Runtime.Services.Graphics;
        var galaxy = _session.World.Galaxy;

        var navigation =
            _session.Player.GalaxyNavigation;

        // Затемняем игровой мир.
        graphics.Submit(
            new DrawUiRectangleCommand(
                new Vector2(0, 0),
                new Vector2(1280, 720),
                new UiColor(5, 9, 22, 235)));

        // Заголовок.
        DrawGalaxyText(
            "GALACTIC NAVIGATION",
            new Vector2(35, 30),
            new DebugColor(100, 200, 255),
            3.0f);

        DrawGalaxyText(
            $"FUEL: {navigation.Fuel}/{navigation.FuelCapacity}",
            new Vector2(35, 80),
            DebugColor.White);

        DrawGalaxyText(
            $"CURRENT SYSTEM: {navigation.CurrentSystemId.ToUpperInvariant()}",
            new Vector2(35, 110),
            DebugColor.Green);

        // Гиперпереходы.
        foreach (var system in galaxy.Systems)
        {
            var start = GetGalaxyScreenPosition(system);

            foreach (var connectionId in system.Connections)
            {
                if (string.CompareOrdinal(
                        system.Id,
                        connectionId) >= 0)
                {
                    continue;
                }

                var connected = galaxy.GetSystem(connectionId);

                if (connected is null)
                    continue;

                var end = GetGalaxyScreenPosition(connected);

                graphics.Submit(
                    new DrawDebugLineCommand(
                        start,
                        end,
                        new DebugColor(35, 90, 145),
                        DebugRenderSpace.Screen));
            }
        }

        // Звёздные системы.
        foreach (var system in galaxy.Systems)
        {
            var position = GetGalaxyScreenPosition(system);

            var isCurrent =
                system.Id == navigation.CurrentSystemId;

            var isSelected =
                system.Id == _selectedGalaxySystemId;

            var color = isCurrent
                ? new DebugColor(80, 255, 130)
                : isSelected
                    ? new DebugColor(255, 210, 80)
                    : new DebugColor(100, 180, 255);

            DrawGalaxyCircle(
                position,
                isSelected ? 13.0f : 8.0f,
                color);

            DrawGalaxyText(
                system.Name,
                new Vector2(
                    position.X + 16,
                    position.Y - 7),
                color,
                1.5f);
        }

        // Информация о выбранной системе.
        var selected = galaxy.GetSystem(
            _selectedGalaxySystemId);

        if (selected is not null)
        {
            var panelPosition = new Vector2(900, 190);

            graphics.Submit(
                new DrawDebugLineCommand(
                    new Vector2(880, 170),
                    new Vector2(880, 470),
                    new DebugColor(50, 110, 160),
                    DebugRenderSpace.Screen));

            DrawGalaxyText(
                "SYSTEM INFORMATION",
                panelPosition,
                new DebugColor(100, 200, 255),
                2.0f);

            DrawGalaxyText(
                selected.Name,
                new Vector2(900, 230),
                DebugColor.White,
                2.0f);

            var isConnected = galaxy.CanTravel(
                navigation.CurrentSystemId,
                selected.Id);

            DrawGalaxyText(
                isConnected
                    ? "HYPERSPACE: AVAILABLE"
                    : "HYPERSPACE: UNAVAILABLE",
                new Vector2(900, 280),
                isConnected
                    ? DebugColor.Green
                    : DebugColor.Red);

            DrawGalaxyText(
                $"JUMP COST: {GalaxyJumpFuelCost} FUEL",
                new Vector2(900, 320),
                DebugColor.White);

            DrawGalaxyText(
                $"REMAINING: {navigation.Fuel} FUEL",
                new Vector2(900, 350),
                DebugColor.White);

            DrawGalaxyText(
                selected.Id == navigation.CurrentSystemId
                    ? "CURRENT LOCATION"
                    : isConnected && navigation.Fuel >= GalaxyJumpFuelCost
                        ? "PRESS ENTER TO JUMP"
                        : "JUMP NOT AVAILABLE",
                new Vector2(900, 410),
                new DebugColor(255, 210, 80));
        }

        DrawGalaxyText(
            _galaxyMapMessage,
            new Vector2(35, 640),
            new DebugColor(100, 220, 255));

        DrawGalaxyText(
            "M - CLOSE MAP    LEFT CLICK - SELECT    ENTER - HYPERSPACE JUMP",
            new Vector2(35, 675),
            DebugColor.White,
            1.5f);
    }

    private void DrawGalaxyText(
    string text,
    Vector2 position,
    DebugColor color,
    float scale = 2.0f)
    {
        Runtime.Services.Graphics.Submit(
            new DrawDebugTextCommand(
                text,
                position,
                color,
                scale,
                DebugRenderSpace.Screen));
    }

    private void DrawGalaxyCircle(
        Vector2 center,
        float radius,
        DebugColor color)
    {
        const int segments = 24;

        var graphics = Runtime.Services.Graphics;

        var previous = new Vector2(
            center.X + radius,
            center.Y);

        for (var i = 1; i <= segments; i++)
        {
            var angle =
                Math.PI * 2.0 * i / segments;

            var current = new Vector2(
                center.X + (float)Math.Cos(angle) * radius,
                center.Y + (float)Math.Sin(angle) * radius);

            graphics.Submit(
                new DrawDebugLineCommand(
                    previous,
                    current,
                    color,
                    DebugRenderSpace.Screen));

            previous = current;
        }
    }

    private static Vector2 GetGalaxyScreenPosition(
    GalaxySystem system)
    {
        return new Vector2(
            GalaxyMapCenterX + system.MapX * GalaxyMapScale,
            GalaxyMapCenterY + system.MapY * GalaxyMapScale);
    }

    private GalaxySystem? FindGalaxySystemAt(
    Vector2 mousePosition)
    {
        if (_session is null)
            return null;

        foreach (var system in _session.World.Galaxy.Systems)
        {
            var position = GetGalaxyScreenPosition(system);

            var dx = mousePosition.X - position.X;
            var dy = mousePosition.Y - position.Y;

            if (dx * dx + dy * dy <= 18.0f * 18.0f)
                return system;
        }

        return null;
    }

    private void HandleGalaxyMapInput()
    {
        if (_session is null || _pointerInput is null)
            return;

        if (_pointerInput.IsPressed(InputMouseButton.Left))
        {
            var hovered = FindGalaxySystemAt(
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

        var graphics = Runtime.Services.Graphics;

        const float panelWidth = 300.0f;
        const float panelHeight = 110.0f;

        var panelPosition = new Vector2(24.0f, 80.0f);

        graphics.Submit(
            new DrawUiRectangleCommand(
                panelPosition,
                new Vector2(panelWidth, panelHeight),
                new UiColor(20, 25, 40, 230)));

        // Текст добавим следующим шагом,
        // после проверки доступного API шрифтов.
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
        _hoveredPlanet = GetHoveredPlanet();
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

        if (_isGalaxyMapOpen)
        {
            DrawGalaxyMap();
        }
        if (_isLanded)
        {
            DrawPlanetScreen();
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
            Runtime.Services.Content.Unload<ShipDefinition>(
                StarterShipAssetPath);

            _starterShipDefinition = null;
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