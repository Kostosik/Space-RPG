using GameProject.Content;

namespace GameProject.NPC;

public sealed class NpcArchetypeDefinition : IGameDefinition
{
    public string Id { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public float SpawnWeight { get; init; } = 1f;

    public List<NpcShipOptionDefinition> ShipOptions { get; init; } = [];

    public NpcLongRangeDefinition StartingCredits { get; init; } = new();

    public NpcFloatRangeDefinition StartingFuelRatio { get; init; } = new()
    {
        Min = 1f,
        Max = 1f
    };

    public NpcFloatRangeDefinition StartingHullCondition { get; init; } = new()
    {
        Min = 1f,
        Max = 1f
    };

    public Dictionary<string, NpcFloatRangeDefinition> PersonalityTraits
    { get; init; } = new(StringComparer.Ordinal);
}

public sealed class NpcShipOptionDefinition
{
    public string ShipDefinitionId { get; init; } = string.Empty;

    public float Weight { get; init; } = 1f;
}

public sealed class NpcLongRangeDefinition
{
    public long Min { get; init; }

    public long Max { get; init; }
}

public sealed class NpcFloatRangeDefinition
{
    public float Min { get; init; }

    public float Max { get; init; }
}