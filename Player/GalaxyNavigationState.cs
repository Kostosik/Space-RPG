namespace GameProject.Player;

public sealed class GalaxyNavigationState
{
    public const int DefaultFuelCapacity = 100;

    private int _fuelCapacity = DefaultFuelCapacity;

    public int Fuel { get; private set; } = DefaultFuelCapacity;

    public int FuelCapacity => _fuelCapacity;

    public void SetFuelCapacity(int capacity)
    {
        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(capacity),
                "Fuel capacity must be greater than zero.");
        }

        _fuelCapacity = capacity;
        Fuel = Math.Min(Fuel, _fuelCapacity);
    }

    public bool TryConsumeFuel(int amount)
    {
        if (amount <= 0 || Fuel < amount)
            return false;

        Fuel -= amount;
        return true;
    }

    public void AddFuel(int amount)
    {
        if (amount <= 0)
            return;

        Fuel = (int)Math.Min(
            FuelCapacity,
            (long)Fuel + amount);
    }
}