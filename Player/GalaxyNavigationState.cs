
using GameProject.World;

namespace GameProject.Player;

public sealed class GalaxyNavigationState
{
    public const int DefaultFuelCapacity = 100;

    private int _fuelCapacity = DefaultFuelCapacity;

    public string CurrentSystemId { get; private set; } = "sol";

    public int Fuel { get; private set; } = DefaultFuelCapacity;

    public int FuelCapacity => _fuelCapacity;

    /// <summary>
    /// Updates the maximum fuel capacity provided by the player's ship.
    /// Existing fuel is preserved, but cannot exceed the new capacity.
    /// </summary>
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

    public bool TryJump(
        GalaxyMap galaxy,
        string targetSystemId,
        int fuelCost = 10)
    {
        ArgumentNullException.ThrowIfNull(galaxy);

        if (fuelCost <= 0)
            return false;

        if (Fuel < fuelCost)
            return false;

        if (!galaxy.CanTravel(
                CurrentSystemId,
                targetSystemId))
        {
            return false;
        }

        Fuel -= fuelCost;
        CurrentSystemId = targetSystemId;

        return true;
    }

    public void AddFuel(int amount)
    {
        if (amount <= 0)
            return;

        Fuel = Math.Min(
            FuelCapacity,
            Fuel + amount);
    }
}
