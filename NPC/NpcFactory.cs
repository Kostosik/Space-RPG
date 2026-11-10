using GameProject.Ships;

namespace GameProject.NPC;

public static class NpcFactory
{
    public static NpcState Create(
        NpcArchetypeDefinition archetype,
        ShipDefinition shipDefinition,
        NpcIdentity identity,
        NpcLocation location)
    {
        ArgumentNullException.ThrowIfNull(archetype);
        ArgumentNullException.ThrowIfNull(shipDefinition);
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentNullException.ThrowIfNull(location);

        NpcArchetypeDefinitionValidator.Validate(archetype);
        ShipDefinitionValidator.Validate(shipDefinition);

        if (!string.Equals(
                archetype.ShipDefinitionId,
                shipDefinition.Id,
                StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                $"NPC archetype '{archetype.Id}' references " +
                $"ship '{archetype.ShipDefinitionId}', " +
                $"but received '{shipDefinition.Id}'.");
        }

        var fuelCapacity =
            shipDefinition.BaseStats.FuelCapacity;

        var initialFuel = (int)Math.Round(
            fuelCapacity * (double)archetype.StartingFuelRatio,
            MidpointRounding.AwayFromZero);

        var personality = new NpcPersonality(
            archetype.PersonalityTraits);

        var ship = new NpcShipState(
            shipDefinition.Id,
            fuelCapacity,
            initialFuel,
            archetype.StartingHullCondition);

        var economy = new NpcEconomyState(
            archetype.StartingCredits);

        return new NpcState(
            identity,
            archetype.Id,
            personality,
            ship,
            location,
            economy);
    }
}