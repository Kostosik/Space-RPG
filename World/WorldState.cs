namespace GameProject.World;

public sealed class WorldState
{
    private WorldState(Guid id, int seed)
    {
        if (id == Guid.Empty)
            throw new ArgumentException(
                "World ID cannot be empty.",
                nameof(id));

        Id = id;
        Seed = seed;

        Galaxy = GalaxyGenerator.Generate(seed);

        var startingSystem = Galaxy.GetSystem("sol")
            ?? throw new InvalidOperationException(
                "Starting galaxy system was not generated.");

        StarSystem = StarSystemGenerator.Generate(
            startingSystem.Seed);
    }

    public Guid Id { get; }

    public int Seed { get; }

    public GalaxyMap Galaxy { get; }

    public StarSystem StarSystem { get; private set; }

    public void EnterSystem(string systemId)
    {
        var system = Galaxy.GetSystem(systemId);

        if (system is null)
        {
            throw new ArgumentException(
                $"Unknown galaxy system: {systemId}",
                nameof(systemId));
        }

        StarSystem = StarSystemGenerator.Generate(
            system.Seed);
    }

    public static WorldState Create(
        WorldStartData startData)
    {
        return new WorldState(
            Guid.NewGuid(),
            startData.Seed);
    }

    public static WorldState CreateNew()
    {
        return Create(WorldStartData.CreateNew());
    }
}