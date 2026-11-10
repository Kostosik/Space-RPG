using GameProject.Content;

namespace GameProject.Ships;

/// <summary>
/// Static template of an equipment model.
/// Multiple individual items can be generated from one definition.
/// </summary>
public sealed class EquipmentDefinition : IGameDefinition
{
    public string Id { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string Type { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public List<EquipmentLevelDefinition> Levels { get; init; } = [];
}