namespace GameProject.World;

/// <summary>
/// Logical state of the game world.
/// Separate from Engine.Worlds.World.
/// </summary>
public sealed class WorldState
{
    public StarSystem StarSystem { get; }
    private WorldState(
        Guid id,
        int seed)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "World ID cannot be empty.",
                nameof(id));
        }

        Id = id;
        Seed = seed;
        StarSystem = StarSystemGenerator.Generate();
    }

    public Guid Id { get; }

    public int Seed { get; }

    public static WorldState Create(
        WorldStartData startData)
    {
        return new WorldState(
            Guid.NewGuid(),
            startData.Seed);
    }

    public static WorldState CreateNew()
    {
        return Create(
            WorldStartData.CreateNew());
    }
}