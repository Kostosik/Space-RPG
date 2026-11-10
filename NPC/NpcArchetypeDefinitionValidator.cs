namespace GameProject.NPC;

public static class NpcArchetypeDefinitionValidator
{
    public static void Validate(NpcArchetypeDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        ValidateId(definition.Id, "ID");
        ValidateId(definition.Name, "Name");
        ValidateId(
            definition.ShipDefinitionId,
            "ShipDefinitionId");

        if (definition.StartingCredits < 0)
        {
            throw new InvalidDataException(
                $"NPC archetype '{definition.Id}' has invalid starting credits.");
        }

        ValidateRatio(
            definition.StartingFuelRatio,
            nameof(definition.StartingFuelRatio));

        ValidateRatio(
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

            ValidateRatio(
                trait.Value,
                $"Personality trait '{trait.Key}'");
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

    private static void ValidateRatio(
        float value,
        string fieldName)
    {
        if (!float.IsFinite(value) ||
            value < 0f ||
            value > 1f)
        {
            throw new InvalidDataException(
                $"Invalid value for '{fieldName}'. Expected 0..1.");
        }
    }
}