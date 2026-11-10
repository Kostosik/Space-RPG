namespace GameProject.Shops;

public sealed class ShopStockEntryDefinition
{
    public string EquipmentDefinitionId { get; init; } = string.Empty;

    public int MinLevel { get; init; } = 1;

    public int MaxLevel { get; init; } = 1;

    public int MinQuantity { get; init; } = 1;

    public int MaxQuantity { get; init; } = 1;
}