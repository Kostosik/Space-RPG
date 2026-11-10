using GameProject.Ships;

namespace GameProject.Shops;

/// <summary>
/// Runtime stock of a shop.
/// Contains unique equipment instances.
/// </summary>
public sealed class ShopStock
{
    private readonly EquipmentContainer _equipment = new();

    public ShopStock(string shopDefinitionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shopDefinitionId);

        if (!string.Equals(
                shopDefinitionId,
                shopDefinitionId.Trim(),
                StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "Shop definition ID cannot contain surrounding whitespace.",
                nameof(shopDefinitionId));
        }

        ShopDefinitionId = shopDefinitionId;
    }

    public string ShopDefinitionId { get; }

    public int Count => _equipment.Count;

    public IReadOnlyDictionary<Guid, EquipmentInstance> Items =>
        _equipment.Items;

    public bool TryGet(
        Guid instanceId,
        out EquipmentInstance? equipment)
    {
        return _equipment.TryGet(instanceId, out equipment);
    }

    internal bool TryAdd(EquipmentInstance equipment)
    {
        return _equipment.TryAdd(equipment);
    }

    public bool TryRemove(
        Guid instanceId,
        out EquipmentInstance? equipment)
    {
        return _equipment.TryRemove(instanceId, out equipment);
    }
}