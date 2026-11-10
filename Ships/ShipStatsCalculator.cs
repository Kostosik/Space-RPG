using GameProject.Player;

namespace GameProject.Ships;

public sealed class ShipStatsCalculator
{
    public ShipStats Calculate(
        ShipDefinition definition,
        PlayerShipState ship,
        IReadOnlyDictionary<string, EquipmentDefinition>
            equipmentDefinitions)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(ship);
        ArgumentNullException.ThrowIfNull(equipmentDefinitions);

        ShipDefinitionValidator.Validate(definition);

        if (!string.Equals(
                definition.Id,
                ship.ShipDefinitionId,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Ship state does not match ship definition.");
        }

        var slots = definition.Slots.ToDictionary(
            slot => slot.Id,
            StringComparer.Ordinal);

        var maxSpeed = definition.BaseStats.MaxSpeed;
        long fuelCapacity = definition.BaseStats.FuelCapacity;

        foreach (var installed in ship.InstalledEquipment)
        {
            if (!slots.TryGetValue(installed.Key, out var slot))
            {
                throw new InvalidDataException(
                    $"Unknown ship slot '{installed.Key}'.");
            }

            var instance = installed.Value;

            if (!equipmentDefinitions.TryGetValue(
                    instance.DefinitionId,
                    out var equipment))
            {
                throw new InvalidDataException(
                    $"Unknown equipment '{instance.DefinitionId}'.");
            }

            if (!slot.AllowedEquipmentTypes.Contains(
                    equipment.Type,
                    StringComparer.Ordinal))
            {
                throw new InvalidDataException(
                    $"Equipment '{equipment.Id}' is incompatible " +
                    $"with slot '{slot.Id}'.");
            }

            if (equipment.Levels is null ||
                !equipment.Levels.Any(
                    level => level.Level == instance.Level))
            {
                throw new InvalidDataException(
                    $"Equipment '{equipment.Id}' has an invalid level.");
            }

            if (!float.IsFinite(instance.Stats.MaxSpeedBonus))
            {
                throw new InvalidDataException(
                    $"Equipment instance '{instance.InstanceId}' " +
                    "has an invalid speed bonus.");
            }

            maxSpeed += instance.Stats.MaxSpeedBonus;
            fuelCapacity += instance.Stats.FuelCapacityBonus;
        }

        if (!float.IsFinite(maxSpeed) ||
            maxSpeed <= 0.0f ||
            fuelCapacity <= 0 ||
            fuelCapacity > int.MaxValue)
        {
            throw new InvalidDataException(
                "Calculated ship statistics are invalid.");
        }

        return new ShipStats
        {
            MaxSpeed = maxSpeed,
            FuelCapacity = (int)fuelCapacity
        };
    }
}