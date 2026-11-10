namespace GameProject.Ships;

public sealed class EquipmentGenerator
{
    private readonly Random _random;

    public EquipmentGenerator()
        : this(Random.Shared)
    {
    }

    public EquipmentGenerator(Random random)
    {
        ArgumentNullException.ThrowIfNull(random);

        _random = random;
    }

    public EquipmentInstance Generate(
        EquipmentDefinition definition,
        int level)
    {
        ArgumentNullException.ThrowIfNull(definition);

        if (string.IsNullOrWhiteSpace(definition.Id) ||
            string.IsNullOrWhiteSpace(definition.Type))
        {
            throw new InvalidDataException(
                "Equipment definition has an invalid ID or type.");
        }

        if (level <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(level));
        }

        if (definition.Levels is null)
        {
            throw new InvalidDataException(
                $"Equipment '{definition.Id}' has no level definitions.");
        }

        if (definition.Levels.Any(
                entry => entry is null))
        {
            throw new InvalidDataException(
                $"Equipment '{definition.Id}' contains a null level.");
        }

        var matchingLevels = definition.Levels
            .Where(entry => entry.Level == level)
            .Take(2)
            .ToArray();

        if (matchingLevels.Length == 0)
        {
            throw new InvalidDataException(
                $"Level {level} is not defined for equipment '{definition.Id}'.");
        }

        if (matchingLevels.Length > 1)
        {
            throw new InvalidDataException(
                $"Equipment '{definition.Id}' contains duplicate level {level}.");
        }

        var levelDefinition = matchingLevels[0];

        var ranges = levelDefinition.Stats
            ?? throw new InvalidDataException(
                $"Equipment '{definition.Id}' has no stat ranges.");

        var speedRange = ranges.MaxSpeedBonus
            ?? throw new InvalidDataException(
                "Maximum speed bonus range is missing.");

        var fuelRange = ranges.FuelCapacityBonus
            ?? throw new InvalidDataException(
                "Fuel capacity bonus range is missing.");

        var stats = new EquipmentStats(
            Roll(speedRange),
            Roll(fuelRange));

        return new EquipmentInstance(
            Guid.NewGuid(),
            definition.Id,
            level,
            stats);
    }

    private float Roll(EquipmentFloatRange range)
    {
        if (!float.IsFinite(range.Min) ||
            !float.IsFinite(range.Max) ||
            range.Min > range.Max)
        {
            throw new InvalidDataException(
                "Equipment contains an invalid floating-point range.");
        }

        var value =
            (double)range.Min +
            _random.NextDouble() *
            ((double)range.Max - range.Min);

        var result = (float)value;

        if (!float.IsFinite(result))
        {
            throw new InvalidDataException(
                "Generated equipment statistic is invalid.");
        }

        return result;
    }

    private int Roll(EquipmentIntRange range)
    {
        if (range.Min > range.Max)
        {
            throw new InvalidDataException(
                "Equipment contains an invalid integer range.");
        }

        return (int)_random.NextInt64(
            range.Min,
            (long)range.Max + 1L);
    }
}