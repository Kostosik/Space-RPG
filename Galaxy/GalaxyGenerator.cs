using System;
using System.Collections.Generic;

namespace GameProject.World;

public static class GalaxyGenerator
{
    public const int DefaultSystemCount = 20;

    private const float BaseMapMinX = -600.0f;
    private const float BaseMapMaxX = 380.0f;
    private const float BaseMapMinY = -420.0f;
    private const float BaseMapMaxY = 420.0f;

    private const float MinSystemDistance = 95.0f;
    private const int MaxPositionAttempts = 20000;

    private const int MaxConnectionsPerSystem = 4;

    private static readonly string[] NamePrefixes =
    [
        "Альфа",
        "Бета",
        "Гамма",
        "Дельта",
        "Эпсилон",
        "Зета",
        "Эта",
        "Тета",
        "Йота",
        "Каппа",
        "Лямбда",
        "Мю",
        "Ню",
        "Кси",
        "Омикрон",
        "Пи"
    ];

    private static readonly string[] ConstellationNames =
    [
        "Центавра",
        "Лиры",
        "Кита",
        "Эридана",
        "Ориона",
        "Дракона",
        "Лебедя",
        "Кассиопеи",
        "Пегаса",
        "Феникса",
        "Гидры",
        "Волопаса",
        "Андромеды",
        "Геркулеса",
        "Персея",
        "Жертвенника"
    ];

    private readonly record struct MapBounds(
        float MinX,
        float MaxX,
        float MinY,
        float MaxY);

    public static GalaxyMap Generate(
        int worldSeed,
        int systemCount = DefaultSystemCount)
    {
        if (systemCount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(systemCount),
                "Galaxy must contain at least one system.");
        }

        var layoutScale = MathF.Sqrt(
            (float)systemCount / DefaultSystemCount);

        var bounds = new MapBounds(
            BaseMapMinX * layoutScale,
            BaseMapMaxX * layoutScale,
            BaseMapMinY * layoutScale,
            BaseMapMaxY * layoutScale);

        var positionRandom = new Random(worldSeed);
        var galaxy = new GalaxyMap();

        var systems = new GalaxySystem[systemCount];

        var usedNames = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase);

        usedNames.Add("Солнечная система");

        for (var i = 0; i < systems.Length; i++)
        {
            var position = i == 0
                ? (X: 0.0f, Y: 0.0f)
                : GeneratePosition(
                    positionRandom,
                    systems,
                    i,
                    bounds);

            var id = i == 0
                ? "sol"
                : $"system-{i:D3}";

            var name = i == 0
                ? "Солнечная система"
                : GenerateSystemName(
                    worldSeed,
                    i,
                    usedNames);

            // Seed системы зависит от Seed галактики и её индекса.
            var systemSeed = unchecked(worldSeed + i + 1);

            var system = new GalaxySystem(
                id,
                name,
                position.X,
                position.Y,
                systemSeed);

            systems[i] = system;
            galaxy.AddSystem(system);
        }

        ConnectMinimumSpanningTree(galaxy, systems);
        AddAlternativeConnections(galaxy, systems);

        return galaxy;
    }

    private static string GenerateSystemName(
        int worldSeed,
        int systemIndex,
        HashSet<string> usedNames)
    {
        var nameSeed = unchecked(
            worldSeed * 397 ^
            systemIndex * 7919 ^
            0x5F3759DF);

        var random = new Random(nameSeed);

        // Повторяем попытки, пока не получим уникальное имя.
        for (var attempt = 0; attempt < 64; attempt++)
        {
            var prefix = NamePrefixes[
                random.Next(NamePrefixes.Length)];

            var constellation = ConstellationNames[
                random.Next(ConstellationNames.Length)];

            var candidate = $"{prefix} {constellation}";

            if (usedNames.Add(candidate))
                return candidate;
        }

        // Гарантированно уникальное резервное имя.
        var fallback = $"Система {systemIndex:D3}";
        usedNames.Add(fallback);

        return fallback;
    }

    private static (float X, float Y) GeneratePosition(
        Random random,
        GalaxySystem[] systems,
        int count,
        MapBounds bounds)
    {
        var minDistanceSquared =
            MinSystemDistance * MinSystemDistance;

        for (var attempt = 0;
             attempt < MaxPositionAttempts;
             attempt++)
        {
            var x = bounds.MinX +
                    random.NextSingle() *
                    (bounds.MaxX - bounds.MinX);

            var y = bounds.MinY +
                    random.NextSingle() *
                    (bounds.MaxY - bounds.MinY);

            var valid = true;

            for (var i = 0; i < count; i++)
            {
                var dx = x - systems[i].MapX;
                var dy = y - systems[i].MapY;

                if (dx * dx + dy * dy < minDistanceSquared)
                {
                    valid = false;
                    break;
                }
            }

            if (valid)
                return (x, y);
        }

        throw new InvalidOperationException(
            $"Unable to generate a valid galaxy layout for {systems.Length} systems.");
    }

    private static void ConnectMinimumSpanningTree(
        GalaxyMap galaxy,
        GalaxySystem[] systems)
    {
        var count = systems.Length;

        if (count <= 1)
            return;

        var connected = new bool[count];
        var bestDistances = new double[count];
        var nearestSystems = new int[count];

        for (var i = 0; i < count; i++)
        {
            bestDistances[i] = double.PositiveInfinity;
            nearestSystems[i] = -1;
        }

        bestDistances[0] = 0.0;

        // Алгоритм Прима: гарантированно связывает все системы.
        // Сложность O(n²), без полного перебора дерева на каждом шаге.
        for (var step = 0; step < count; step++)
        {
            var current = -1;
            var currentBestDistance = double.PositiveInfinity;

            for (var i = 0; i < count; i++)
            {
                if (connected[i])
                    continue;

                if (bestDistances[i] >= currentBestDistance)
                    continue;

                current = i;
                currentBestDistance = bestDistances[i];
            }

            if (current < 0)
            {
                throw new InvalidOperationException(
                    "Unable to connect all galaxy systems.");
            }

            connected[current] = true;

            if (nearestSystems[current] >= 0)
            {
                galaxy.Connect(
                    systems[nearestSystems[current]].Id,
                    systems[current].Id);
            }

            for (var candidate = 0;
                 candidate < count;
                 candidate++)
            {
                if (connected[candidate])
                    continue;

                var distance = GetDistanceSquared(
                    systems[current],
                    systems[candidate]);

                if (distance >= bestDistances[candidate])
                    continue;

                bestDistances[candidate] = distance;
                nearestSystems[candidate] = current;
            }
        }
    }

    private static void AddAlternativeConnections(
        GalaxyMap galaxy,
        GalaxySystem[] systems)
    {
        var candidates =
            new List<(int First, int Second, double DistanceSquared)>();

        for (var first = 0; first < systems.Length; first++)
        {
            for (var second = first + 1;
                 second < systems.Length;
                 second++)
            {
                if (HasConnection(
                        systems[first],
                        systems[second].Id))
                {
                    continue;
                }

                candidates.Add((
                    first,
                    second,
                    GetDistanceSquared(
                        systems[first],
                        systems[second])));
            }
        }

        candidates.Sort(static (a, b) =>
        {
            var comparison =
                a.DistanceSquared.CompareTo(b.DistanceSquared);

            if (comparison != 0)
                return comparison;

            comparison = a.First.CompareTo(b.First);

            return comparison != 0
                ? comparison
                : a.Second.CompareTo(b.Second);
        });

        // Число дополнительных связей растёт вместе с галактикой.
        var targetExtraConnections = systems.Length / 2;
        var addedConnections = 0;

        foreach (var candidate in candidates)
        {
            if (addedConnections >= targetExtraConnections)
                break;

            var first = systems[candidate.First];
            var second = systems[candidate.Second];

            if (first.Connections.Count >= MaxConnectionsPerSystem ||
                second.Connections.Count >= MaxConnectionsPerSystem)
            {
                continue;
            }

            if (galaxy.Connect(first.Id, second.Id))
                addedConnections++;
        }
    }

    private static bool HasConnection(
        GalaxySystem system,
        string targetId)
    {
        foreach (var connection in system.Connections)
        {
            if (string.Equals(
                    connection,
                    targetId,
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static double GetDistanceSquared(
        GalaxySystem first,
        GalaxySystem second)
    {
        var dx = (double)first.MapX - second.MapX;
        var dy = (double)first.MapY - second.MapY;

        return dx * dx + dy * dy;
    }
}