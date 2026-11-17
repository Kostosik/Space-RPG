namespace GameProject.Economy;

public static class TradeGoodDefinitionValidator
{
    public static void Validate(TradeGoodDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        if (string.IsNullOrWhiteSpace(definition.Id) ||
            !string.Equals(
                definition.Id,
                definition.Id.Trim(),
                StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                "Trade good has an invalid ID.");
        }

        if (string.IsNullOrWhiteSpace(definition.Name))
        {
            throw new InvalidDataException(
                $"Trade good '{definition.Id}' has no name.");
        }

        if (definition.BasePrice <= 0)
        {
            throw new InvalidDataException(
                $"Trade good '{definition.Id}' has an invalid base price.");
        }
    }
}