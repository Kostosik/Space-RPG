using GameProject.Player;

namespace GameProject.Ships;

public sealed class ShipStatsCalculator
{
    public ShipStats Calculate(
        ShipDefinition definition,
        PlayerShipState ship,
        IReadOnlyDictionary<string, EquipmentDefinition> equipmentDefinitions)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(ship);
        ArgumentNullException.ThrowIfNull(equipmentDefinitions);

        if (!string.Equals(
                definition.Id,
                ship.ShipDefinitionId,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Ship state does not match ship definition.");
        }

        if (definition.BaseStats is null ||
            !float.IsFinite(definition.BaseStats.MaxSpeed) ||
            definition.BaseStats.MaxSpeed <= 0.0f ||
            definition.BaseStats.FuelCapacity <= 0)
        {
            throw new InvalidDataException(
                "Ship contains invalid base statistics.");
        }

        var slots = definition.Slots.ToDictionary(
            slot => slot.Id,
            StringComparer.Ordinal);

        var maxSpeed = definition.BaseStats.MaxSpeed;
        var fuelCapacity = definition.BaseStats.FuelCapacity;

        foreach (var installed in ship.InstalledEquipment)
        {
            if (!slots.TryGetValue(installed.Key, out var slot))
            {
                throw new InvalidDataException(
                    $"Unknown ship slot '{installed.Key}'.");
            }

            if (!equipmentDefinitions.TryGetValue(
                    installed.Value,
                    out var equipment))
            {
                throw new InvalidDataException(
                    $"Unknown equipment '{installed.Value}'.");
            }

            if (!string.Equals(
                    slot.Type,
                    equipment.Type,
                    StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    $"Equipment '{equipment.Id}' is incompatible " +
                    $"with slot '{slot.Id}'.");
            }

            if (!float.IsFinite(equipment.MaxSpeedBonus))
            {
                throw new InvalidDataException(
                    $"Equipment '{equipment.Id}' has invalid speed bonus.");
            }

            maxSpeed += equipment.MaxSpeedBonus;
            fuelCapacity += equipment.FuelCapacityBonus;
        }

        if (!float.IsFinite(maxSpeed) ||
            maxSpeed <= 0.0f ||
            fuelCapacity <= 0)
        {
            throw new InvalidDataException(
                "Calculated ship statistics are invalid.");
        }

        return new ShipStats
        {
            MaxSpeed = maxSpeed,
            FuelCapacity = fuelCapacity
        };
    }
}