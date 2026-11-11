using GameProject.NPC;
using GameProject.Navigation;
using GameProject.Ships;
using GameProject.World;

namespace GameProject.Simulation;

public sealed class NpcMovementSimulation
{
    private const double ApproachClearance = 32.0;
    private const double ArrivalTolerance = 4.0;
    private const double RouteRefreshDistance = 10.0;
    private const double PathEpsilon = 0.000001;
    private const double ShipTurnRadius = 30.0;
    private const int MinimumPlanetStopsBeforeJump = 2;
    private const double SystemDepartureChance = 0.30;
    private sealed class CachedSystem
    {
        public CachedSystem(
            StarSystem starSystem,
            TimeSpan lastAdvancedAt)
        {
            StarSystem = starSystem;
            LastAdvancedAt = lastAdvancedAt;
        }

        public StarSystem StarSystem { get; }

        public TimeSpan LastAdvancedAt { get; set; }
    }

    private sealed class NpcRoute
    {
        public NpcRoute(
            NavigationPath path,
            NavigationVector2 destination)
        {
            Path = path;
            Destination = destination;
        }

        public NavigationPath Path { get; }

        public NavigationVector2 Destination { get; }

        public int NextWaypointIndex { get; set; } = 1;
    }

    private readonly WorldState _world;

    private readonly Dictionary<string, float> _shipSpeeds =
        new(StringComparer.Ordinal);

    private readonly Dictionary<Guid, NpcRandom> _random =
        new();

    private readonly Dictionary<Guid, NpcRoute> _routes =
        new();

    // Геометрия систем, в которых сейчас нет игрока.
    // Это не ECS-сущности и не локальные игровые сцены.
    private readonly Dictionary<string, CachedSystem> _systemCache =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly NavigationPathfinder _pathfinder = new();

    public NpcMovementSimulation(
        WorldState world,
        IReadOnlyDictionary<string, ShipDefinition> shipDefinitions)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(shipDefinitions);

        _world = world;

        foreach (var definition in shipDefinitions.Values)
        {
            var speed = definition.BaseStats.MaxSpeed;

            if (!float.IsFinite(speed) || speed <= 0.0f)
            {
                throw new InvalidDataException(
                    $"Ship '{definition.Id}' has invalid NPC movement speed.");
            }

            _shipSpeeds.Add(definition.Id, speed);
        }

        var currentTime = _world.ElapsedWorldTime;

        foreach (var npc in _world.Npcs.All.OrderBy(x => x.Id))
        {
            if (!_shipSpeeds.ContainsKey(npc.Ship.ShipDefinitionId))
            {
                throw new InvalidDataException(
                    $"Unknown NPC ship '{npc.Ship.ShipDefinitionId}'.");
            }

            var randomSeed = NpcDeterminism.CreateSeed(
                _world.Seed,
                npc.Id,
                "movement");

            _random.Add(
                npc.Id,
                new NpcRandom(randomSeed));
        }
    }

    public void Advance(
        TimeSpan delta,
        TimeSpan currentElapsed)
    {
        if (delta < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(delta));
        }

        if (delta == TimeSpan.Zero)
            return;

        foreach (var npc in _world.Npcs.All.OrderBy(x => x.Id))
        {
            var system = GetSystem(
                npc.Location.SystemId,
                currentElapsed);

            AdvanceNpc(
                npc,
                system,
                delta,
                currentElapsed);
        }
    }
  

    private void AdvanceNpc(
    NpcState npc,
    StarSystem system,
    TimeSpan delta,
    TimeSpan currentElapsed)
    {
        var movement = npc.Movement;

        // Межсистемный маршрут имеет приоритет:
        // сначала NPC должен долететь до границы.
        if (movement.TargetSystemId is { } targetSystemId)
        {
            AdvanceToSystemExit(
                npc,
                system,
                targetSystemId,
                delta,
                currentElapsed);

            return;
        }

        if (movement.TargetObjectId is not { } targetObjectId)
            return;

        var target = FindTargetPlanet(
            system,
            targetObjectId);

        if (target is null)
        {
            movement.ClearTarget();
            _routes.Remove(npc.Id);
            return;
        }

        var destination = GetApproachPosition(
            target,
            movement.TargetAngleRadians);

        // Прибывший корабль остаётся рядом с движущейся планетой.
        if (movement.IsArrived)
        {
            SetNpcLocation(
                npc,
                destination,
                target.Id);

            return;
        }

        var mustRefreshRoute =
            !_routes.TryGetValue(npc.Id, out var route) ||
            NavigationVector2.Distance(
                route.Destination,
                destination) >= RouteRefreshDistance;

        if (mustRefreshRoute)
        {
            TryBuildRoute(
                npc,
                system,
                target,
                destination);
        }

        if (!_routes.TryGetValue(npc.Id, out route))
            return;

        var speed = _shipSpeeds[npc.Ship.ShipDefinitionId];

        var distanceBudget =
            speed * delta.TotalDays;

        AdvanceAlongRoute(
            npc,
            system,
            target,
            route,
            destination,
            distanceBudget);
    }

    public bool TryAssignSystemTarget(Guid npcId)
    {
        var npc = _world.Npcs.Get(npcId);

        if (npc is null ||
            (npc.Movement.HasTarget &&
             !npc.Movement.IsArrived))
        {
            return false;
        }

        var currentSystem = _world.Galaxy.GetSystem(
            npc.Location.SystemId);

        if (currentSystem is null ||
            currentSystem.Connections.Count == 0)
        {
            return false;
        }

        var candidates = currentSystem.Connections
            .Where(id => !string.Equals(
                id,
                npc.Movement.LastSystemId,
                StringComparison.OrdinalIgnoreCase))
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();

        // Если других направлений нет, разрешаем возвращение.
        if (candidates.Length == 0)
        {
            candidates = currentSystem.Connections
                .OrderBy(id => id, StringComparer.Ordinal)
                .ToArray();
        }

        if (candidates.Length == 0)
            return false;

        var random = _random[npc.Id];

        var startIndex = (int)(
            random.NextDouble() * candidates.Length);

        var system = GetSystem(
            npc.Location.SystemId,
            _world.ElapsedWorldTime);

        for (var offset = 0; offset < candidates.Length; offset++)
        {
            var index = (startIndex + offset) % candidates.Length;
            var targetSystemId = candidates[index];

            npc.Movement.SetSystemTarget(targetSystemId);
            npc.SetLocation(
    new NpcLocation(
        npc.Location.SystemId,
        npc.Location.X,
        npc.Location.Y));
            _routes.Remove(npc.Id);

            if (TryBuildSystemExitRoute(
                    npc,
                    system,
                    targetSystemId))
            {
                return true;
            }

            npc.Movement.ClearTarget();
        }

        return false;
    }

    private bool TryBuildSystemExitRoute(
    NpcState npc,
    StarSystem system,
    string targetSystemId)
    {
        if (!TryGetSystemExitPosition(
                npc.Location.SystemId,
                targetSystemId,
                system,
                out var destination))
        {
            return false;
        }

        return TryBuildPath(
            npc,
            system,
            destination);
    }

    private bool TryGetSystemExitPosition(
        string sourceSystemId,
        string targetSystemId,
        StarSystem sourceSystem,
        out NavigationVector2 destination)
    {
        destination = default;

        var source = _world.Galaxy.GetSystem(sourceSystemId);
        var target = _world.Galaxy.GetSystem(targetSystemId);

        if (source is null ||
            target is null ||
            !_world.Galaxy.CanTravel(source.Id, target.Id))
        {
            return false;
        }

        var dx = (double)target.MapX - source.MapX;
        var dy = (double)target.MapY - source.MapY;

        var length = Math.Sqrt(dx * dx + dy * dy);

        if (!double.IsFinite(length) || length <= PathEpsilon)
            return false;

        var exitRadius =
            sourceSystem.JumpBoundaryRadius -
            StarSystem.DefaultJumpBoundaryClearance;

        if (!double.IsFinite(exitRadius) ||
            exitRadius <= 0.0)
        {
            return false;
        }

        destination = new NavigationVector2(
            sourceSystem.Star.X + dx / length * exitRadius,
            sourceSystem.Star.Y + dy / length * exitRadius);

        return destination.IsFinite;
    }

    private bool TryBuildPath(
        NpcState npc,
        StarSystem system,
        NavigationVector2 destination)
    {
        var start = new NavigationVector2(
            npc.Location.X,
            npc.Location.Y);

        var rawPath = _pathfinder.FindPath(
            start,
            destination,
            system.NavigationObstacles);

        if (rawPath is null)
            return false;

        var smoothPath = NavigationPathSmoother.Smooth(
            rawPath,
            npc.Movement.Rotation,
            ShipTurnRadius,
            system.NavigationObstacles,
            _pathfinder);

        var path = smoothPath ?? rawPath;

        if (path.Length <= PathEpsilon)
            return false;

        _routes[npc.Id] = new NpcRoute(
            path,
            destination);

        return true;
    }

    private bool TryBuildRoute(
    NpcState npc,
    StarSystem system,
    PlanetObject target,
    NavigationVector2 destination)
    {
        var start = new NavigationVector2(
            npc.Location.X,
            npc.Location.Y);

        if (NavigationVector2.Distance(start, destination)
            <= ArrivalTolerance)
        {
            Arrive(npc, target, destination);
            return true;
        }

        return TryBuildPath(
            npc,
            system,
            destination);
    }

    private void AdvanceToSystemExit(
    NpcState npc,
    StarSystem sourceSystem,
    string targetSystemId,
    TimeSpan delta,
    TimeSpan currentElapsed)
    {
        if (!_routes.TryGetValue(npc.Id, out var route))
        {
            if (!TryBuildSystemExitRoute(
                    npc,
                    sourceSystem,
                    targetSystemId))
            {
                npc.Movement.ClearTarget();
                _routes.Remove(npc.Id);
                return;
            }

            if (!_routes.TryGetValue(npc.Id, out route))
                return;
        }

        var speed = _shipSpeeds[npc.Ship.ShipDefinitionId];
        var distanceBudget = speed * delta.TotalDays;

        var current = new NavigationVector2(
            npc.Location.X,
            npc.Location.Y);

        while (distanceBudget > PathEpsilon &&
               route.NextWaypointIndex < route.Path.Points.Count)
        {
            var waypoint =
                route.Path.Points[route.NextWaypointIndex];

            var direction = waypoint - current;
            var segmentLength = direction.Length;

            if (segmentLength <= PathEpsilon)
            {
                current = waypoint;
                route.NextWaypointIndex++;
                continue;
            }

            direction /= segmentLength;

            npc.Movement.SetRotation(
                (float)Math.Atan2(
                    direction.Y,
                    direction.X));

            var travelDistance = Math.Min(
                distanceBudget,
                segmentLength);

            current += direction * travelDistance;
            distanceBudget -= travelDistance;

            if (travelDistance >= segmentLength - PathEpsilon)
            {
                current = waypoint;
                route.NextWaypointIndex++;
            }
        }

        SetNpcLocation(npc, current, null);

        if (route.NextWaypointIndex >= route.Path.Points.Count)
        {
            CompleteSystemJump(
                npc,
                targetSystemId,
                currentElapsed);
        }
    }

    private void CompleteSystemJump(
        NpcState npc,
        string targetSystemId,
        TimeSpan currentElapsed)
    {
        var sourceSystemId = npc.Location.SystemId;

        var source = _world.Galaxy.GetSystem(sourceSystemId);
        var target = _world.Galaxy.GetSystem(targetSystemId);

        if (source is null ||
            target is null ||
            !_world.Galaxy.CanTravel(sourceSystemId, targetSystemId))
        {
            npc.Movement.ClearTarget();
            _routes.Remove(npc.Id);
            return;
        }

        var dx = (double)target.MapX - source.MapX;
        var dy = (double)target.MapY - source.MapY;

        var length = Math.Sqrt(dx * dx + dy * dy);

        if (!double.IsFinite(length) || length <= PathEpsilon)
        {
            npc.Movement.ClearTarget();
            _routes.Remove(npc.Id);
            return;
        }

        var destinationSystem = GetSystem(
            targetSystemId,
            currentElapsed);

        // Тот же принцип появления, что и у корабля игрока:
        // входная сторона обращена к системе отправления.
        var arrivalRadius =
            destinationSystem.JumpBoundaryRadius -
            StarSystem.DefaultArrivalBoundaryOffset;

        var spawnPosition = new NavigationVector2(
            destinationSystem.Star.X - dx / length * arrivalRadius,
            destinationSystem.Star.Y - dy / length * arrivalRadius);

        npc.Movement.CompleteSystemTransition(sourceSystemId);

        npc.Movement.SetRotation(
            (float)Math.Atan2(dy, dx));

        npc.SetLocation(
            new NpcLocation(
                targetSystemId,
                spawnPosition.X,
                spawnPosition.Y));

        _routes.Remove(npc.Id);
    }

    public bool TryAssignPlanetTarget(Guid npcId)
    {
        var npc = _world.Npcs.Get(npcId);

        if (npc is null ||
            (npc.Movement.HasTarget &&
             !npc.Movement.IsArrived))
        {
            return false;
        }

        var system = GetSystem(
            npc.Location.SystemId,
            _world.ElapsedWorldTime);

        var planets = system.Objects
            .OfType<PlanetObject>()
            .OrderBy(x => x.Id, StringComparer.Ordinal)
            .ToArray();

        if (planets.Length == 0)
            return false;

        var random = _random[npc.Id];

        var startIndex = Math.Min(
            planets.Length - 1,
            (int)(random.NextDouble() * planets.Length));

        var previousObjectId = npc.Location.ObjectId;

        for (var offset = 0; offset < planets.Length; offset++)
        {
            var index = (startIndex + offset) % planets.Length;
            var planet = planets[index];

            if (planets.Length > 1 &&
                string.Equals(
                    planet.Id,
                    previousObjectId,
                    StringComparison.Ordinal))
            {
                continue;
            }

            var angle = random.NextDouble() * Math.Tau;

            npc.Movement.SetTarget(
                planet.Id,
                angle);

            npc.SetLocation(
                new NpcLocation(
                    npc.Location.SystemId,
                    npc.Location.X,
                    npc.Location.Y));

            _routes.Remove(npc.Id);

            var destination = GetApproachPosition(
                planet,
                angle);

            if (TryBuildRoute(
                    npc,
                    system,
                    planet,
                    destination))
            {
                return true;
            }

            npc.Movement.ClearTarget();
        }

        return false;
    }

    private void AdvanceAlongRoute(
        NpcState npc,
        StarSystem system,
        PlanetObject target,
        NpcRoute route,
        NavigationVector2 currentDestination,
        double distanceBudget)
    {
        var current = new NavigationVector2(
            npc.Location.X,
            npc.Location.Y);

        while (distanceBudget > PathEpsilon &&
               route.NextWaypointIndex < route.Path.Points.Count)
        {
            var waypoint =
                route.Path.Points[route.NextWaypointIndex];

            var direction = waypoint - current;
            var segmentLength = direction.Length;

            if (segmentLength <= PathEpsilon)
            {
                current = waypoint;
                route.NextWaypointIndex++;
                continue;
            }

            direction /= segmentLength;

            npc.Movement.SetRotation(
                (float)Math.Atan2(
                    direction.Y,
                    direction.X));

            var travelDistance = Math.Min(
                distanceBudget,
                segmentLength);

            current += direction * travelDistance;
            distanceBudget -= travelDistance;

            if (travelDistance >= segmentLength - PathEpsilon)
            {
                current = waypoint;
                route.NextWaypointIndex++;
            }
        }

        SetNpcLocation(npc, current, null);

        if (route.NextWaypointIndex < route.Path.Points.Count)
            return;

        var remainingDistance =
            NavigationVector2.Distance(
                current,
                currentDestination);

        if (remainingDistance <= ArrivalTolerance)
        {
            Arrive(
                npc,
                target,
                currentDestination);

            return;
        }

        // Если планета успела сместиться, а старый маршрут завершён,
        // строим новый путь к её актуальному положению.
        TryBuildRoute(
            npc,
            system,
            target,
            currentDestination);
    }

    private void Arrive(
        NpcState npc,
        PlanetObject target,
        NavigationVector2 destination)
    {
        SetNpcLocation(
            npc,
            destination,
            target.Id);

        npc.Movement.MarkArrived();
        npc.Movement.RegisterPlanetStop();

        _routes.Remove(npc.Id);
    }

    private static void SetNpcLocation(
        NpcState npc,
        NavigationVector2 position,
        string? objectId)
    {
        npc.SetLocation(
            new NpcLocation(
                npc.Location.SystemId,
                position.X,
                position.Y,
                objectId));
    }

    private static NavigationVector2 GetApproachPosition(
        PlanetObject planet,
        double angle)
    {
        var distance =
            planet.VisualRadius + ApproachClearance;

        return new NavigationVector2(
            planet.X + Math.Cos(angle) * distance,
            planet.Y + Math.Sin(angle) * distance);
    }

    private static PlanetObject? FindTargetPlanet(
        StarSystem system,
        string objectId)
    {
        return system.Objects
            .OfType<PlanetObject>()
            .FirstOrDefault(
                x => string.Equals(
                    x.Id,
                    objectId,
                    StringComparison.Ordinal));
    }

    // Для непосещённых систем создаётся только геометрия,
    // необходимая глобальной симуляции NPC.
    // При входе игрока используем настоящий локальный StarSystem.
    private StarSystem GetSystem(
        string systemId,
        TimeSpan elapsedWorldTime)
    {
        if (string.Equals(
                systemId,
                _world.CurrentSystemId,
                StringComparison.OrdinalIgnoreCase))
        {
            // Если игрок вошёл в систему, используем её настоящее
            // локальное состояние, а старую копию из кэша удаляем.
            _systemCache.Remove(systemId);

            return _world.StarSystem;
        }

        if (!_systemCache.TryGetValue(
                systemId,
                out var cached))
        {
            var definition = _world.Galaxy.GetSystem(systemId)
                ?? throw new InvalidOperationException(
                    $"Unknown NPC system '{systemId}'.");

            var starSystem = StarSystemGenerator.Generate(
                definition.Id,
                definition.Seed);

            // Сразу устанавливаем правильное положение орбит
            // на текущем игровом времени.
            starSystem.AdvanceOrbit(
                elapsedWorldTime.TotalDays);

            cached = new CachedSystem(
                starSystem,
                elapsedWorldTime);

            _systemCache.Add(systemId, cached);

            return starSystem;
        }

        if (elapsedWorldTime < cached.LastAdvancedAt)
        {
            throw new InvalidOperationException(
                "NPC system time cannot move backwards.");
        }

        var elapsed = elapsedWorldTime - cached.LastAdvancedAt;

        if (elapsed > TimeSpan.Zero)
        {
            cached.StarSystem.AdvanceOrbit(
                elapsed.TotalDays);

            cached.LastAdvancedAt = elapsedWorldTime;
        }

        return cached.StarSystem;
    }
}