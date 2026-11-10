
using System.Collections.Generic;

namespace GameProject.World;

public static class StarSystemGenerator
{
    private static readonly string[] PlanetNames =
    [
        "Эридан",
        "Нова",
        "Кассини",
        "Терра",
        "Арктур",
        "Валькирия",
        "Зенит",
        "Пандора",
        "Эос",
        "Немезида"
    ];

    public static StarSystem Generate()
    {
        var seed = Random.Shared.Next();

        return Generate($"generated-{seed}", seed);
    }

    public static StarSystem Generate(int seed)
    {
        return Generate($"generated-{seed}", seed);
    }

    public static StarSystem Generate(string systemId, int seed)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(systemId);

        if (!string.Equals(
                systemId,
                systemId.Trim(),
                StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "System ID cannot contain surrounding whitespace.",
                nameof(systemId));
        }

        var random = new Random(seed);

        var star = new StarObject(
            $"{systemId}:star",
            "Звезда",
            0,
            0,
            28,
            60);

        var objects = new List<StarSystemObject>
        {
            star
        };

        var planetCount = random.Next(3, 7);

        for (var i = 0; i < planetCount; i++)
        {
            var distance =
                115.0 + i * 80.0 + random.NextDouble() * 20.0;

            var angle =
                random.NextDouble() * Math.PI * 2.0;

            var orbitalPeriod = 12.0 + i * 9.0;

            var radius = random.Next(12, 24);

            var name = PlanetNames[
                random.Next(PlanetNames.Length)];

            objects.Add(
                new PlanetObject(
                    $"{systemId}:planet:{i + 1}",
                    name,
                    distance,
                    orbitalPeriod,
                    angle,
                    radius,
                    "Неисследованная планета."));
        }

        var asteroidCount = random.Next(4, 10);

        for (var i = 0; i < asteroidCount; i++)
        {
            objects.Add(
                new AsteroidObject(
                    $"{systemId}:asteroid:{i + 1}",
                    "Астероид",
                    random.Next(-350, 351),
                    random.Next(-350, 351),
                    random.Next(3, 7)));
        }

        objects.Add(
            new StationObject(
                $"{systemId}:station:1",
                "Орбитальная станция",
                random.Next(-280, 281),
                random.Next(-280, 281),
                11,
                "Космическая станция."));

        return new StarSystem(star, objects);
    }
}
