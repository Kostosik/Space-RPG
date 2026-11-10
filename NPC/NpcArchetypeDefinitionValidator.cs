namespace GameProject.NPC;

public static class NpcArchetypeDefinitionValidator
{
    public static void Validate(NpcArchetypeDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        ValidateId(definition.Id, "ID");
        ValidateId(definition.Name, "Name");

        if (!float.IsFinite(definition.SpawnWeight) ||
            definition.SpawnWeight <= 0f)
        {
            throw new InvalidDataException(
                $"NPC archetype '{definition.Id}' has invalid spawn weight.");
        }

        if (definition.ShipOptions is null ||
            definition.ShipOptions.Count == 0)
        {
            throw new InvalidDataException(
                $"NPC archetype '{definition.Id}' has no ship options.");
        }

        var shipIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var option in definition.ShipOptions)
        {
            if (option is null)
            {
                throw new InvalidDataException(
                    $"NPC archetype '{definition.Id}' has a null ship option.");
            }

            ValidateId(
                option.ShipDefinitionId,
                "ShipDefinitionId");

            if (!shipIds.Add(option.ShipDefinitionId))
            {
                throw new InvalidDataException(
                    $"NPC archetype '{definition.Id}' contains duplicate ship " +
                    $"'{option.ShipDefinitionId}'.");
            }

            if (!float.IsFinite(option.Weight) ||
                option.Weight <= 0f)
            {
                throw new InvalidDataException(
                    $"NPC archetype '{definition.Id}' has invalid ship weight.");
            }
        }

        if (definition.StartingCredits is null ||
            definition.StartingCredits.Min < 0 ||
            definition.StartingCredits.Max < definition.StartingCredits.Min)
        {
            throw new InvalidDataException(
                $"NPC archetype '{definition.Id}' has invalid credits range.");
        }

        ValidateRange(
            definition.StartingFuelRatio,
            nameof(definition.StartingFuelRatio));

        ValidateRange(
            definition.StartingHullCondition,
            nameof(definition.StartingHullCondition));

        if (definition.PersonalityTraits is null)
        {
            throw new InvalidDataException(
                $"NPC archetype '{definition.Id}' has null personality traits.");
        }

        foreach (var trait in definition.PersonalityTraits)
        {
            ValidateId(trait.Key, "Personality trait ID");

            ValidateRange(
                trait.Value,
                $"Personality trait '{trait.Key}'");
        }
    }

    private static void ValidateRange(
        NpcFloatRangeDefinition range,
        string fieldName)
    {
        if (range is null ||
            !float.IsFinite(range.Min) ||
            !float.IsFinite(range.Max) ||
            range.Min < 0f ||
            range.Max > 1f ||
            range.Min > range.Max)
        {
            throw new InvalidDataException(
                $"Invalid range for '{fieldName}'. Expected 0..1.");
        }
    }

    private static void ValidateId(
        string value,
        string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value) ||
            !string.Equals(
                value,
                value.Trim(),
                StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                $"NPC archetype has invalid {fieldName}.");
        }
    }
}