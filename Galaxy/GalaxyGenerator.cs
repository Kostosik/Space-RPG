
using System;
using System.Collections.Generic;

namespace GameProject.World;

public static class GalaxyGenerator
{
    private const float MapMinX = -600.0f;
    private const float MapMaxX = 380.0f;
    private const float MapMinY = -420.0f;
    private const float MapMaxY = 420.0f;

    private const float MinSystemDistance = 95.0f;
    private const int MaxPositionAttempts = 20000;

    private const int ExtraConnectionsCount = 10;
    private const int MaxConnectionsPerSystem = 4;

    private static readonly (string Id, string Name)[] SystemDefinitions =
    [
        ("sol", "Солнечная система"),
        ("alpha", "Альфа Центавра"),
        ("vega", "Вега"),
        ("sirius", "Сириус"),
        ("proxima", "Проксима"),
        ("kepler", "Кеплер"),
        ("arcturus", "Арктур"),
        ("rigel", "Ригель"),
        ("betelgeuse", "Бетельгейзе"),
        ("canopus", "Канопус"),
        ("altair", "Альтаир"),
        ("deneb", "Денеб"),
        ("antares", "Антарес"),
        ("castor", "Кастор"),
        ("pollux", "Поллукс"),
        ("tau-ceti", "Тау Кита"),
        ("epsilon-eridani", "Эпсилон Эридана"),
        ("procyon", "Процион"),
        ("barnard", "Звезда Барнарда"),
        ("wolf-359", "Вольф 359")
    ];

    public static GalaxyMap Generate(int worldSeed)
    {
        var random = new Random(worldSeed);
        var galaxy = new GalaxyMap();

        var systems = new GalaxySystem[SystemDefinitions.Length];

        for (var i = 0; i < systems.Length; i++)
        {
            var definition = SystemDefinitions[i];

            var position = i == 0
                ? (X: 0.0f, Y: 0.0f)
                : GeneratePosition(random, systems, i);

            var systemSeed = unchecked(worldSeed + i + 1);

            var system = new GalaxySystem(
                definition.Id,
                definition.Name,
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

    private static (float X, float Y) GeneratePosition(
        Random random,
        GalaxySystem[] systems,
        int count)
    {
        for (var attempt = 0; attempt < MaxPositionAttempts; attempt++)
        {
            var x = MapMinX +
                    random.NextSingle() * (MapMaxX - MapMinX);

            var y = MapMinY +
                    random.NextSingle() * (MapMaxY - MapMinY);

            var valid = true;

            for (var i = 0; i < count; i++)
            {
                var dx = x - systems[i].MapX;
                var dy = y - systems[i].MapY;

                if (dx * dx + dy * dy <
                    MinSystemDistance * MinSystemDistance)
                {
                    valid = false;
                    break;
                }
            }

            if (valid)
                return (x, y);
        }

        throw new InvalidOperationException(
            "Unable to generate a valid galaxy layout.");
    }

    private static void ConnectMinimumSpanningTree(
        GalaxyMap galaxy,
        GalaxySystem[] systems)
    {
        var connected = new bool[systems.Length];
        connected[0] = true;

        for (var edge = 0; edge < systems.Length - 1; edge++)
        {
            var bestFrom = -1;
            var bestTo = -1;
            var bestDistance = double.PositiveInfinity;

            for (var from = 0; from < systems.Length; from++)
            {
                if (!connected[from])
                    continue;

                for (var to = 0; to < systems.Length; to++)
                {
                    if (connected[to])
                        continue;

                    var distance = GetDistanceSquared(
                        systems[from],
                        systems[to]);

                    if (distance >= bestDistance)
                        continue;

                    bestDistance = distance;
                    bestFrom = from;
                    bestTo = to;
                }
            }

            if (bestFrom < 0 || bestTo < 0)
            {
                throw new InvalidOperationException(
                    "Unable to connect all galaxy systems.");
            }

            galaxy.Connect(
                systems[bestFrom].Id,
                systems[bestTo].Id);

            connected[bestTo] = true;
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
                if (HasConnection(systems[first], systems[second].Id))
                    continue;

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

        var addedConnections = 0;

        foreach (var candidate in candidates)
        {
            if (addedConnections >= ExtraConnectionsCount)
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
