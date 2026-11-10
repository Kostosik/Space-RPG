namespace GameProject.World;

/// <summary>
/// Parameters used to create a new game world.
/// </summary>
public readonly record struct WorldStartData(
    int Seed,
    int GalaxySystemCount = GalaxyGenerator.DefaultSystemCount)
{
    public static WorldStartData CreateNew(
        int galaxySystemCount = GalaxyGenerator.DefaultSystemCount)
    {
        return new WorldStartData(
            Random.Shared.Next(),
            galaxySystemCount);
    }
}