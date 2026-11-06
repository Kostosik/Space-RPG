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
        return Generate(Random.Shared.Next());
    }

    public static StarSystem Generate(int seed)
    {
        var random = new Random(seed);

        var star = new StarObject(
            "Звезда",
            0,
            0,
            18,
            45);

        var objects = new List<StarSystemObject>
        {
            star
        };

        var planetCount = random.Next(3, 7);

        for (var i = 0; i < planetCount; i++)
        {
            var distance = 100.0 + i * 75.0 +
                           random.NextDouble() * 35.0;

            var angle = random.NextDouble() *
                        Math.PI * 2.0;

            var x = Math.Cos(angle) * distance;
            var y = Math.Sin(angle) * distance;

            var radius = random.Next(6, 16);

            var name = PlanetNames[
                random.Next(PlanetNames.Length)];

            objects.Add(
                new PlanetObject(
                    name,
                    x,
                    y,
                    radius,
                    "Неисследованная планета."));

        }

        // Астероидное поле.
        var asteroidCount = random.Next(4, 10);

        for (var i = 0; i < asteroidCount; i++)
        {
            objects.Add(
                new AsteroidObject(
                    "Астероид",
                    random.Next(-350, 351),
                    random.Next(-350, 351),
                    random.Next(3, 7)));
        }

        // Космическая станция.
        objects.Add(
            new StationObject(
                "Орбитальная станция",
                random.Next(-280, 281),
                random.Next(-280, 281),
                11,
                "Космическая станция."));

        return new StarSystem(star, objects);
    }
}