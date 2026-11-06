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
            28,
            60);

        var objects = new List<StarSystemObject>
        {
            star
        };

        var planetCount = random.Next(3, 7);

        for (var i = 0; i < planetCount; i++)
        {
            // Радиус орбиты.
            var distance =
                115.0 + i * 80.0 + random.NextDouble() * 20.0;

            // Начальное положение.
            var angle =
                random.NextDouble() * Math.PI * 2.0;

            // У каждой планеты своя скорость.
            var orbitalPeriod = 12.0 + i * 9.0;

            // Увеличиваем размер планет.
            var radius = random.Next(12, 24);

            var name = PlanetNames[
                random.Next(PlanetNames.Length)];

            objects.Add(
                new PlanetObject(
                    name,
                    distance,
                    orbitalPeriod,
                    angle,
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