using System.Collections.Generic;

namespace GameProject.World;

public static class StarSystemGenerator
{
    public static StarSystem Generate()
    {
        var star = new StarObject(
            "Звезда",
            0,
            0,
            18,
            45);

        var objects = new List<StarSystemObject>
        {
            star,

            new PlanetObject(
                "Эридан",
                120,
                30,
                8,
                "Небольшая каменистая планета."),

            new PlanetObject(
                "Нова",
                -190,
                80,
                13,
                "Планета с плотной атмосферой."),

            new AsteroidObject(
                "Астероид",
                70,
                -100,
                4),

            new StationObject(
                "Станция Альфа",
                240,
                -60,
                10,
                "Торговая космическая станция.")
        };

        return new StarSystem(star, objects);
    }
}