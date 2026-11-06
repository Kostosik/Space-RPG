using GameProject.Content;

namespace GameProject.Ships;

/// <summary>
/// Static definition of a ship model.
/// Describes the hull, base stats and available equipment slots.
/// </summary>
public sealed class ShipDefinition : IGameDefinition
{
    public string Id { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string ShipClass { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public ShipBaseStats BaseStats { get; init; } = new();

    public List<ShipSlotDefinition> Slots { get; init; } = [];
}