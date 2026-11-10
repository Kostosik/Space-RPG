using GameProject.Player;

namespace GameProject.Ships;

public sealed class ShipEquipmentService
{
    public EquipmentTransferResult TryInstallFromCargoHold(
        PlayerShipState ship,
        ShipDefinition shipDefinition,
        string slotId,
        Guid equipmentInstanceId,
        IReadOnlyDictionary<string, EquipmentDefinition>
            equipmentDefinitions)
    {
        ArgumentNullException.ThrowIfNull(ship);
        ArgumentNullException.ThrowIfNull(shipDefinition);
        ArgumentException.ThrowIfNullOrWhiteSpace(slotId);
        ArgumentNullException.ThrowIfNull(equipmentDefinitions);

        ShipDefinitionValidator.Validate(shipDefinition);

        if (!string.Equals(
                ship.ShipDefinitionId,
                shipDefinition.Id,
                StringComparison.Ordinal))
        {
            return EquipmentTransferResult.ShipDefinitionMismatch;
        }

        if (!ship.CargoHold.TryGet(
                equipmentInstanceId,
                out var equipmentInstance) ||
            equipmentInstance is null)
        {
            return EquipmentTransferResult.EquipmentNotFoundInCargoHold;
        }

        if (!equipmentDefinitions.TryGetValue(
                equipmentInstance.DefinitionId,
                out var equipmentDefinition) ||
            equipmentDefinition is null)
        {
            return EquipmentTransferResult.EquipmentDefinitionNotFound;
        }

        if (!string.Equals(
                equipmentDefinition.Id,
                equipmentInstance.DefinitionId,
                StringComparison.Ordinal))
        {
            return EquipmentTransferResult.EquipmentDefinitionMismatch;
        }

        if (equipmentDefinition.Levels is null ||
            !equipmentDefinition.Levels.Any(
                level => level is not null &&
                         level.Level == equipmentInstance.Level))
        {
            return EquipmentTransferResult.InvalidEquipmentLevel;
        }

        var slot = shipDefinition.Slots.FirstOrDefault(
            candidate => string.Equals(
                candidate.Id,
                slotId,
                StringComparison.Ordinal));

        if (slot is null)
            return EquipmentTransferResult.SlotNotFound;

        if (!slot.AllowedEquipmentTypes.Contains(
                equipmentDefinition.Type,
                StringComparer.Ordinal))
        {
            return EquipmentTransferResult.IncompatibleSlot;
        }

        if (ship.InstalledEquipment.ContainsKey(slot.Id))
            return EquipmentTransferResult.SlotOccupied;

        if (ship.InstalledEquipment.Values.Any(
                installed =>
                    installed.InstanceId == equipmentInstanceId))
        {
            return EquipmentTransferResult.EquipmentAlreadyInstalled;
        }

        if (!ship.CargoHold.TryRemove(
                equipmentInstanceId,
                out var removedEquipment) ||
            removedEquipment is null)
        {
            return EquipmentTransferResult.EquipmentNotFoundInCargoHold;
        }

        if (ship.TrySetEquipment(slot.Id, removedEquipment))
        {
            ship.NotifyEquipmentChanged();
            return EquipmentTransferResult.Success;
        }

        if (!ship.CargoHold.TryAdd(removedEquipment))
        {
            throw new InvalidOperationException(
                "Failed to restore equipment to cargo hold.");
        }

        return EquipmentTransferResult.SlotOccupied;
    }

    public EquipmentTransferResult TryRemoveToCargoHold(
        PlayerShipState ship,
        string slotId)
    {
        ArgumentNullException.ThrowIfNull(ship);
        ArgumentException.ThrowIfNullOrWhiteSpace(slotId);

        if (!ship.InstalledEquipment.TryGetValue(
                slotId,
                out var equipment) ||
            equipment is null)
        {
            return EquipmentTransferResult.EquipmentNotInstalled;
        }

        if (ship.CargoHold.Items.ContainsKey(equipment.InstanceId))
        {
            return EquipmentTransferResult.EquipmentAlreadyInCargoHold;
        }

        if (!ship.TryRemoveEquipment(
                slotId,
                out var removedEquipment) ||
            removedEquipment is null)
        {
            return EquipmentTransferResult.EquipmentNotInstalled;
        }

        if (ship.CargoHold.TryAdd(removedEquipment))
        {
            ship.NotifyEquipmentChanged();
            return EquipmentTransferResult.Success;
        }

        if (!ship.TrySetEquipment(slotId, removedEquipment))
        {
            throw new InvalidOperationException(
                "Failed to restore equipment to its original slot.");
        }

        return EquipmentTransferResult.CargoHoldRejected;
    }
}