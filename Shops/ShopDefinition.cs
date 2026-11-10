using GameProject.Content;

namespace GameProject.Shops;

/// <summary>
/// Static configuration of a shop's possible assortment.
/// </summary>
public sealed class ShopDefinition : IGameDefinition
{
    public string Id { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public List<ShopStockEntryDefinition> Stock { get; init; } = [];

    public double PurchasePriceMultiplier { get; init; } = 1.0;

    public double SellPriceMultiplier { get; init; } = 0.5;
}