using System.Collections.ObjectModel;

namespace GameProject.Player;

/// <summary>
/// Persistent state of the player's current ship.
/// Stores the selected ship model and installed equipment.
/// </summary>
public sealed class PlayerShipState
{
    private readonly Dictionary<string, string> _installedEquipment = new(
        StringComparer.Ordinal);

    private readonly ReadOnlyDictionary<string, string>
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
            new ReadOnlyDictionary<string, string>(
                _installedEquipment);
    }

    public string ShipDefinitionId { get; }

    public IReadOnlyDictionary<string, string> InstalledEquipment =>
        _readOnlyInstalledEquipment;

    internal void SetEquipment(
        string slotId,
        string equipmentDefinitionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slotId);
        ArgumentException.ThrowIfNullOrWhiteSpace(equipmentDefinitionId);

        _installedEquipment[slotId] = equipmentDefinitionId;
    }

    internal bool RemoveEquipment(string slotId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slotId);

        return _installedEquipment.Remove(slotId);
    }
}