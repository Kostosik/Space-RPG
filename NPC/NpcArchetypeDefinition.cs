using GameProject.Content;

namespace GameProject.NPC;

public sealed class NpcArchetypeDefinition : IGameDefinition
{
    public string Id { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string ShipDefinitionId { get; init; } = string.Empty;

    public long StartingCredits { get; init; }

    public float StartingFuelRatio { get; init; } = 1.0f;

    public float StartingHullCondition { get; init; } = 1.0f;

    public Dictionary<string, float> PersonalityTraits { get; init; }
        = new(StringComparer.Ordinal);
}