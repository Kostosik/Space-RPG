namespace GameProject.World;

public static class GalaxyGenerator
{
    public static GalaxyMap Generate(int worldSeed)
    {
        var galaxy = new GalaxyMap();

        galaxy.AddSystem(new GalaxySystem(
            "sol", "Солнечная система",
            0, 0, worldSeed + 1));

        galaxy.AddSystem(new GalaxySystem(
            "alpha", "Альфа Центавра",
            180, -70, worldSeed + 2));

        galaxy.AddSystem(new GalaxySystem(
            "vega", "Вега",
            350, 40, worldSeed + 3));

        galaxy.AddSystem(new GalaxySystem(
            "sirius", "Сириус",
            150, 180, worldSeed + 4));

        galaxy.AddSystem(new GalaxySystem(
            "proxima", "Проксима",
            -150, 100, worldSeed + 5));

        galaxy.AddSystem(new GalaxySystem(
            "kepler", "Кеплер",
            -300, -120, worldSeed + 6));

        galaxy.Connect("sol", "alpha");
        galaxy.Connect("sol", "sirius");
        galaxy.Connect("sol", "proxima");

        galaxy.Connect("alpha", "vega");
        galaxy.Connect("alpha", "sirius");

        galaxy.Connect("proxima", "kepler");
        galaxy.Connect("kepler", "sirius");

        return galaxy;
    }
}