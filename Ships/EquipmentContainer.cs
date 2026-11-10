using System.Collections.ObjectModel;

namespace GameProject.Ships;

/// <summary>
/// Stores unique equipment instances without stacking them.
/// The same item instance must be transferred between containers,
/// never recreated during a transfer.
/// </summary>
public sealed class EquipmentContainer
{
    private readonly Dictionary<Guid, EquipmentInstance> _items = new();

    private readonly ReadOnlyDictionary<Guid, EquipmentInstance>
        _readOnlyItems;

    public EquipmentContainer()
    {
        _readOnlyItems =
            new ReadOnlyDictionary<Guid, EquipmentInstance>(_items);
    }

    public int Count => _items.Count;

    public IReadOnlyDictionary<Guid, EquipmentInstance> Items =>
        _readOnlyItems;

    public bool TryGet(
        Guid instanceId,
        out EquipmentInstance? equipment)
    {
        if (_items.TryGetValue(instanceId, out var found))
        {
            equipment = found;
            return true;
        }

        equipment = null;
        return false;
    }

    public bool TryAdd(EquipmentInstance equipment)
    {
        ArgumentNullException.ThrowIfNull(equipment);

        return _items.TryAdd(
            equipment.InstanceId,
            equipment);
    }

    public bool TryRemove(
        Guid instanceId,
        out EquipmentInstance? equipment)
    {
        if (_items.Remove(instanceId, out var removed))
        {
            equipment = removed;
            return true;
        }

        equipment = null;
        return false;
    }
}