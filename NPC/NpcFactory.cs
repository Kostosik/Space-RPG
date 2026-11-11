using GameProject.Ships;

namespace GameProject.NPC;

public static class NpcFactory
{
    public static NpcState Create(
        NpcArchetypeDefinition archetype,
        IReadOnlyDictionary<string, ShipDefinition> shipDefinitions,
        NpcIdentity identity,
        NpcLocation location,
        NpcRandom random)
    {
        ArgumentNullException.ThrowIfNull(archetype);
        ArgumentNullException.ThrowIfNull(shipDefinitions);
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentNullException.ThrowIfNull(location);
        ArgumentNullException.ThrowIfNull(random);

        NpcArchetypeDefinitionValidator.Validate(archetype);

        var options = new List<(ShipDefinition Ship, double Weight)>();

        foreach (var option in archetype.ShipOptions
                     .OrderBy(x => x.ShipDefinitionId, StringComparer.Ordinal))
        {
            if (!shipDefinitions.TryGetValue(
                    option.ShipDefinitionId,
                    out var definition))
            {
                throw new InvalidDataException(
                    $"Unknown ship '{option.ShipDefinitionId}' " +
                    $"in archetype '{archetype.Id}'.");
            }

            ShipDefinitionValidator.Validate(definition);

            options.Add((definition, option.Weight));
        }

        var selectedShip = SelectShip(
            options,
            random.Fork("ship"));

        var fuelCapacity =
            selectedShip.BaseStats.FuelCapacity;

        var initialFuel = (int)Math.Round(
            fuelCapacity * Sample(
                archetype.StartingFuelRatio,
                random.Fork("fuel")),
            MidpointRounding.AwayFromZero);

        var personalityTraits =
            new Dictionary<string, float>(StringComparer.Ordinal);

        foreach (var trait in archetype.PersonalityTraits
                     .OrderBy(x => x.Key, StringComparer.Ordinal))
        {
            personalityTraits.Add(
                trait.Key,
                Sample(
                    trait.Value,
                    random.Fork($"trait:{trait.Key}")));
        }

        var personality = new NpcPersonality(personalityTraits);

        var ship = new NpcShipState(
            selectedShip.Id,
            fuelCapacity,
            initialFuel,
            Sample(
                archetype.StartingHullCondition,
                random.Fork("hull")));

        var economy = new NpcEconomyState(
            Sample(
                archetype.StartingCredits,
                random.Fork("credits")));

        return new NpcState(
            identity,
            archetype.Id,
            personality,
            ship,
            location,
            economy);
    }

    private static ShipDefinition SelectShip(
        IReadOnlyList<(ShipDefinition Ship, double Weight)> options,
        NpcRandom random)
    {
        double totalWeight = options.Sum(x => x.Weight);

        if (!double.IsFinite(totalWeight) || totalWeight <= 0)
        {
            throw new InvalidDataException(
                "NPC ship options have invalid total weight.");
        }

        var roll = random.NextDouble() * totalWeight;

        foreach (var option in options)
        {
            roll -= option.Weight;

            if (roll < 0)
                return option.Ship;
        }

        return options[^1].Ship;
    }

    private static float Sample(
        NpcFloatRangeDefinition range,
        NpcRandom random)
    {
        return range.Min +
               (range.Max - range.Min) * random.NextSingle();
    }

    private static long Sample(
        NpcLongRangeDefinition range,
        NpcRandom random)
    {
        var width =
            (decimal)range.Max - range.Min + 1m;

        var offset = decimal.ToInt64(
            decimal.Floor(
                (decimal)random.NextDouble() * width));

        return checked(range.Min + offset);
    }
}