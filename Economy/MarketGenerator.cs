namespace GameProject.Economy;

public static class MarketGenerator
{
    public static MarketState Generate(
        int systemSeed,
        IReadOnlyCollection<TradeGoodDefinition> definitions)
    {
        ArgumentNullException.ThrowIfNull(definitions);

        if (definitions.Count == 0)
        {
            throw new InvalidDataException(
                "Cannot generate an empty market.");
        }

        var random = new Random(systemSeed);
        var market = new MarketState();

        foreach (var definition in definitions
                     .OrderBy(x => x.Id, StringComparer.Ordinal))
        {
            TradeGoodDefinitionValidator.Validate(definition);

            // Индивидуальная базовая цена системы.
            var regionalMultiplier =
                0.75 + random.NextDouble() * 0.50;

            var basePrice = decimal.Round(
                definition.BasePrice * (decimal)regionalMultiplier,
                0,
                MidpointRounding.AwayFromZero);

            var regionalBasePrice = Math.Max(
                1L,
                (long)basePrice);

            var targetStock = random.Next(40, 81);

            var initialStock = random.Next(
                targetStock / 2,
                targetStock * 3 / 2 + 1);

            market.AddOffer(
                new MarketOffer(
                    definition.Id,
                    regionalBasePrice,
                    initialStock,
                    targetStock,
                    targetStock * 2));
        }

        return market;
    }
}