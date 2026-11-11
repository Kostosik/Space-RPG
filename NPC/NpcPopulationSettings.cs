using GameProject.World;

namespace GameProject.NPC;

public sealed class NpcPopulationSettings
{
    public int MinimumPerSystem { get; init; } = 1;

    public int MaximumPerSystem { get; init; } = 3;

    public double MinimumSpawnDistance { get; init; } = 100.0;

    public double MaximumSpawnDistance { get; init; } = 500.0;

    public void Validate()
    {
        if (MinimumPerSystem < 0 ||
            MaximumPerSystem < MinimumPerSystem)
        {
            throw new InvalidDataException(
                "Invalid NPC population range.");
        }

        if (!double.IsFinite(MinimumSpawnDistance) ||
            !double.IsFinite(MaximumSpawnDistance) ||
            MinimumSpawnDistance <= 0 ||
            MaximumSpawnDistance < MinimumSpawnDistance ||
            MaximumSpawnDistance >=
                StarSystem.DefaultJumpBoundaryRadius -
                StarSystem.DefaultJumpBoundaryClearance)
        {
            throw new InvalidDataException(
                "Invalid NPC spawn distance range.");
        }
    }
}