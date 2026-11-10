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

        double totalWeight = 0.0;

        foreach (var option in archetype.ShipOptions)
        {
            if (!shipDefinitions.TryGetValue(
                    option.ShipDefinitionId,
                    out var shipDefinition))
            {
                throw new InvalidDataException(
                    $"NPC archetype '{archetype.Id}' references unknown ship " +
                    $"'{option.ShipDefinitionId}'.");
            }

            ShipDefinitionValidator.Validate(shipDefinition);

            if (!string.Equals(
                    shipDefinition.Id,
                    option.ShipDefinitionId,
                    StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    $"Ship definition ID mismatch: '{option.ShipDefinitionId}'.");
            }

            options.Add((shipDefinition, option.Weight));
            totalWeight += option.Weight;
        }

        if (!double.IsFinite(totalWeight) || totalWeight <= 0.0)
        {
            throw new InvalidDataException(
                $"NPC archetype '{archetype.Id}' has invalid total ship weight.");
        }

        var selectedShip = SelectShip(options, totalWeight, random);

        var fuelCapacity =
            selectedShip.BaseStats.FuelCapacity;

        var fuelRatio = Sample(
            archetype.StartingFuelRatio,
            random);

        var initialFuel = (int)Math.Round(
            fuelCapacity * fuelRatio,
            MidpointRounding.AwayFromZero);

        var personalityTraits =
            new Dictionary<string, float>(StringComparer.Ordinal);

        foreach (var trait in archetype.PersonalityTraits)
        {
            personalityTraits.Add(
                trait.Key,
                Sample(trait.Value, random));
        }

        var personality = new NpcPersonality(personalityTraits);

        var ship = new NpcShipState(
            selectedShip.Id,
            fuelCapacity,
            initialFuel,
            Sample(archetype.StartingHullCondition, random));

        var economy = new NpcEconomyState(
            Sample(archetype.StartingCredits, random));

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
        double totalWeight,
        NpcRandom random)
    {
        var roll = random.NextDouble() * totalWeight;

        foreach (var option in options)
        {
            roll -= option.Weight;

            if (roll < 0.0)
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