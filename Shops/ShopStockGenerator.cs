using GameProject.Ships;

namespace GameProject.Shops;

public sealed class ShopStockGenerator
{
    private readonly Random _random;
    private readonly EquipmentGenerator _equipmentGenerator;

    public ShopStockGenerator()
        : this(Random.Shared)
    {
    }

    public ShopStockGenerator(Random random)
    {
        ArgumentNullException.ThrowIfNull(random);

        _random = random;
        _equipmentGenerator = new EquipmentGenerator(random);
    }

    public ShopStock Generate(
        ShopDefinition definition,
        IReadOnlyDictionary<string, EquipmentDefinition>
            equipmentDefinitions)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(equipmentDefinitions);

        ValidateDefinition(definition);

        var result = new ShopStock();

        foreach (var entry in definition.Stock)
        {
            if (!equipmentDefinitions.TryGetValue(
                    entry.EquipmentDefinitionId,
                    out var equipmentDefinition) ||
                equipmentDefinition is null)
            {
                throw new InvalidDataException(
                    $"Shop '{definition.Id}' references unknown equipment " +
                    $"'{entry.EquipmentDefinitionId}'.");
            }

            var availableLevels = equipmentDefinition.Levels
                .Where(level =>
                    level.Level >= entry.MinLevel &&
                    level.Level <= entry.MaxLevel)
                .Select(level => level.Level)
                .ToArray();

            if (availableLevels.Length == 0)
            {
                throw new InvalidDataException(
                    $"Shop '{definition.Id}' has no available levels for " +
                    $"equipment '{equipmentDefinition.Id}'.");
            }

            var quantity = Roll(
                entry.MinQuantity,
                entry.MaxQuantity);

            for (var i = 0; i < quantity; i++)
            {
                var level = availableLevels[
                    _random.Next(availableLevels.Length)];

                var instance = _equipmentGenerator.Generate(
                    equipmentDefinition,
                    level);

                if (!result.TryAdd(instance))
                {
                    throw new InvalidOperationException(
                        "Generated equipment instance ID collision.");
                }
            }
        }

        return result;
    }

    private static void ValidateDefinition(
        ShopDefinition definition)
    {
        if (string.IsNullOrWhiteSpace(definition.Id) ||
            !string.Equals(
                definition.Id,
                definition.Id.Trim(),
                StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                "Shop definition has an invalid ID.");
        }

        if (string.IsNullOrWhiteSpace(definition.Name))
        {
            throw new InvalidDataException(
                $"Shop '{definition.Id}' has no name.");
        }

        if (definition.Stock is null)
        {
            throw new InvalidDataException(
                $"Shop '{definition.Id}' has no stock configuration.");
        }

        foreach (var entry in definition.Stock)
        {
            if (entry is null ||
                string.IsNullOrWhiteSpace(
                    entry.EquipmentDefinitionId))
            {
                throw new InvalidDataException(
                    $"Shop '{definition.Id}' contains an invalid stock entry.");
            }

            if (!string.Equals(
                    entry.EquipmentDefinitionId,
                    entry.EquipmentDefinitionId.Trim(),
                    StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    "Equipment definition ID contains surrounding whitespace.");
            }

            if (entry.MinLevel <= 0 ||
                entry.MaxLevel < entry.MinLevel)
            {
                throw new InvalidDataException(
                    $"Shop '{definition.Id}' contains an invalid level range.");
            }

            if (entry.MinQuantity < 0 ||
                entry.MaxQuantity < entry.MinQuantity)
            {
                throw new InvalidDataException(
                    $"Shop '{definition.Id}' contains an invalid quantity range.");
            }
        }
    }

    private int Roll(int min, int max)
    {
        return (int)_random.NextInt64(
            min,
            (long)max + 1L);
    }
}