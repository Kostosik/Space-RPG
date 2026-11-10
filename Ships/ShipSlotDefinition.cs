namespace GameProject.Ships;

public sealed class ShipSlotDefinition
{
    public string Id { get; init; } = string.Empty;

    public List<string> AllowedEquipmentTypes { get; init; } = [];
}