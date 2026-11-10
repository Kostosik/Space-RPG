namespace GameProject.NPC;

public sealed class NpcShipState
{
    public NpcShipState(
        string shipDefinitionId,
        int fuelCapacity,
        int fuel,
        float hullCondition = 1f)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            shipDefinitionId);

        if (!string.Equals(
                shipDefinitionId,
                shipDefinitionId.Trim(),
                StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "Ship definition ID cannot contain surrounding whitespace.",
                nameof(shipDefinitionId));
        }

        if (fuelCapacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(fuelCapacity));

        if (fuel < 0 || fuel > fuelCapacity)
            throw new ArgumentOutOfRangeException(nameof(fuel));

        ValidateHullCondition(hullCondition);

        ShipDefinitionId = shipDefinitionId;
        FuelCapacity = fuelCapacity;
        Fuel = fuel;
        HullCondition = hullCondition;
    }

    public string ShipDefinitionId { get; }

    public int Fuel { get; private set; }

    public int FuelCapacity { get; private set; }

    public float HullCondition { get; private set; }

    public bool TryConsumeFuel(int amount)
    {
        if (amount <= 0 || Fuel < amount)
            return false;

        Fuel -= amount;
        return true;
    }

    public int Refuel(int amount)
    {
        if (amount <= 0)
            return 0;

        var added = Math.Min(
            amount,
            FuelCapacity - Fuel);

        Fuel += added;

        return added;
    }

    public void SetFuelCapacity(int capacity)
    {
        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity));

        FuelCapacity = capacity;
        Fuel = Math.Min(Fuel, FuelCapacity);
    }

    public void SetHullCondition(float condition)
    {
        ValidateHullCondition(condition);
        HullCondition = condition;
    }

    private static void ValidateHullCondition(float value)
    {
        if (!float.IsFinite(value) ||
            value < 0f ||
            value > 1f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value));
        }
    }
}