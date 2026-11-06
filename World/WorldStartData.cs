namespace GameProject.World;

/// <summary>
/// Parameters used to create a new game world.
/// </summary>
public readonly record struct WorldStartData(int Seed)
{
    public static WorldStartData CreateNew()
    {
        return new WorldStartData(
            Random.Shared.Next());
    }
}