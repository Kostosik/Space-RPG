using GameProject.Player;
using GameProject.Ships;

namespace GameProject.UI.Ship;

public sealed class ShipWindowPresenter
{
    private readonly ShipStatsCalculator _statsCalculator = new();

    public ShipWindowViewData CreateViewData(
        ShipDefinition shipDefinition,
        PlayerShipState shipState,
        IReadOnlyDictionary<string, EquipmentDefinition> equipmentDefinitions)
    {
        ArgumentNullException.ThrowIfNull(shipDefinition);
        ArgumentNullException.ThrowIfNull(shipState);
        ArgumentNullException.ThrowIfNull(equipmentDefinitions);

        var stats = _statsCalculator.Calculate(
            shipDefinition,
            shipState,
            equipmentDefinitions);

        var slots = shipDefinition.Slots
            .Select(slot =>
            {
                EquipmentItemViewData? installedEquipment = null;

                if (shipState.InstalledEquipment.TryGetValue(
                        slot.Id,
                        out var instance))
                {
                    installedEquipment = CreateEquipmentData(
                        instance,
                        equipmentDefinitions);
                }

                return new ShipSlotViewData(
                    slot.Id,
                    slot.AllowedEquipmentTypes.ToArray(),
                    installedEquipment);
            })
            .ToArray();

        var cargo = shipState.CargoHold.Items.Values
            .Select(instance => CreateEquipmentData(
                instance,
                equipmentDefinitions))
            .ToArray();

        return new ShipWindowViewData(
            shipDefinition.Name,
            shipDefinition.ShipClass,
            stats.MaxSpeed,
            stats.FuelCapacity,
            slots,
            cargo);
    }

    private static EquipmentItemViewData CreateEquipmentData(
        EquipmentInstance instance,
        IReadOnlyDictionary<string, EquipmentDefinition> definitions)
    {
        if (!definitions.TryGetValue(
                instance.DefinitionId,
                out var definition))
        {
            throw new InvalidDataException(
                $"Equipment definition '{instance.DefinitionId}' was not found.");
        }

        return new EquipmentItemViewData(
            instance.InstanceId,
            instance.DefinitionId,
            definition.Name,
            definition.Type,
            definition.Description,
            instance.Level,
            instance.Stats);
    }
}

public sealed record ShipWindowViewData(
    string ShipName,
    string ShipClass,
    float MaxSpeed,
    int FuelCapacity,
    IReadOnlyList<ShipSlotViewData> Slots,
    IReadOnlyList<EquipmentItemViewData> Cargo);

public sealed record ShipSlotViewData(
    string SlotId,
    IReadOnlyList<string> AllowedEquipmentTypes,
    EquipmentItemViewData? InstalledEquipment);

public sealed record EquipmentItemViewData(
    Guid InstanceId,
    string DefinitionId,
    string Name,
    string Type,
    string Description,
    int Level,
    EquipmentStats Stats);