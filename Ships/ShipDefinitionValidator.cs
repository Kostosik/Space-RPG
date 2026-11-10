namespace GameProject.Ships;

public static class ShipDefinitionValidator
{
    public static void Validate(ShipDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        if (string.IsNullOrWhiteSpace(definition.Id) ||
            !string.Equals(
                definition.Id,
                definition.Id.Trim(),
                StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                "Ship definition has an invalid ID.");
        }

        if (definition.BaseStats is null ||
            !float.IsFinite(definition.BaseStats.MaxSpeed) ||
            definition.BaseStats.MaxSpeed <= 0.0f ||
            definition.BaseStats.FuelCapacity <= 0)
        {
            throw new InvalidDataException(
                $"Ship '{definition.Id}' has invalid base statistics.");
        }

        if (definition.Slots is null)
        {
            throw new InvalidDataException(
                $"Ship '{definition.Id}' has no slot configuration.");
        }

        var slotIds = new HashSet<string>(
            StringComparer.Ordinal);

        foreach (var slot in definition.Slots)
        {
            if (slot is null ||
                string.IsNullOrWhiteSpace(slot.Id) ||
                !string.Equals(
                    slot.Id,
                    slot.Id.Trim(),
                    StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    $"Ship '{definition.Id}' contains an invalid slot ID.");
            }

            if (!slotIds.Add(slot.Id))
            {
                throw new InvalidDataException(
                    $"Ship '{definition.Id}' contains duplicate slot ID " +
                    $"'{slot.Id}'.");
            }

            if (slot.AllowedEquipmentTypes is null ||
                slot.AllowedEquipmentTypes.Count == 0)
            {
                throw new InvalidDataException(
                    $"Slot '{slot.Id}' has no allowed equipment types.");
            }

            var types = new HashSet<string>(
                StringComparer.Ordinal);

            foreach (var type in slot.AllowedEquipmentTypes)
            {
                if (string.IsNullOrWhiteSpace(type) ||
                    !string.Equals(
                        type,
                        type.Trim(),
                        StringComparison.Ordinal))
                {
                    throw new InvalidDataException(
                        $"Slot '{slot.Id}' contains an invalid equipment type.");
                }

                if (!types.Add(type))
                {
                    throw new InvalidDataException(
                        $"Slot '{slot.Id}' contains duplicate equipment type " +
                        $"'{type}'.");
                }
            }
        }
    }
}