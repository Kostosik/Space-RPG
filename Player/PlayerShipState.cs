using System.Collections.ObjectModel;
using GameProject.Ships;

namespace GameProject.Player;

public sealed class PlayerShipState
{
    private readonly Dictionary<string, EquipmentInstance>
        _installedEquipment = new(StringComparer.Ordinal);

    private readonly ReadOnlyDictionary<string, EquipmentInstance>
        _readOnlyInstalledEquipment;

    public PlayerShipState(string shipDefinitionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shipDefinitionId);

        if (!string.Equals(
                shipDefinitionId,
                shipDefinitionId.Trim(),
                StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "Ship definition ID cannot contain surrounding whitespace.",
                nameof(shipDefinitionId));
        }

        ShipDefinitionId = shipDefinitionId;

        _readOnlyInstalledEquipment =
            new ReadOnlyDictionary<string, EquipmentInstance>(
                _installedEquipment);
    }

    public string ShipDefinitionId { get; }

    public EquipmentContainer CargoHold { get; } = new();

    public IReadOnlyDictionary<string, EquipmentInstance>
        InstalledEquipment => _readOnlyInstalledEquipment;

    internal event Action? EquipmentChanged;

    internal void NotifyEquipmentChanged()
    {
        EquipmentChanged?.Invoke();
    }

    internal bool TrySetEquipment(
        string slotId,
        EquipmentInstance equipment)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slotId);
        ArgumentNullException.ThrowIfNull(equipment);

        if (_installedEquipment.ContainsKey(slotId))
            return false;

        if (_installedEquipment.Values.Any(
                installed =>
                    installed.InstanceId == equipment.InstanceId))
        {
            return false;
        }

        _installedEquipment.Add(slotId, equipment);

        return true;
    }

    internal bool TryRemoveEquipment(
        string slotId,
        out EquipmentInstance? equipment)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slotId);

        if (_installedEquipment.Remove(
                slotId,
                out var removed))
        {
            equipment = removed;
            return true;
        }

        equipment = null;
        return false;
    }
}