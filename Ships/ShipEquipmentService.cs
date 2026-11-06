using GameProject.Player;

namespace GameProject.Ships;

public sealed class ShipEquipmentService
{
    public bool TryInstall(
        PlayerShipState ship,
        ShipDefinition shipDefinition,
        string slotId,
        EquipmentDefinition equipment)
    {
        ArgumentNullException.ThrowIfNull(ship);
        ArgumentNullException.ThrowIfNull(shipDefinition);
        ArgumentException.ThrowIfNullOrWhiteSpace(slotId);
        ArgumentNullException.ThrowIfNull(equipment);

        if (!string.Equals(
                ship.ShipDefinitionId,
                shipDefinition.Id,
                StringComparison.Ordinal))
        {
            return false;
        }

        var slot = shipDefinition.Slots.FirstOrDefault(
            candidate => string.Equals(
                candidate.Id,
                slotId,
                StringComparison.Ordinal));

        if (slot is null ||
            !string.Equals(
                slot.Type,
                equipment.Type,
                StringComparison.Ordinal))
        {
            return false;
        }

        ship.SetEquipment(slot.Id, equipment.Id);
        return true;
    }

    public bool TryRemove(
        PlayerShipState ship,
        string slotId)
    {
        ArgumentNullException.ThrowIfNull(ship);
        ArgumentException.ThrowIfNullOrWhiteSpace(slotId);

        return ship.RemoveEquipment(slotId);
    }
}