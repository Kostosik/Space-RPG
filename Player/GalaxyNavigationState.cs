using GameProject.World;

namespace GameProject.Player;

public sealed class GalaxyNavigationState
{
    public const int DefaultFuelCapacity = 100;

    public string CurrentSystemId { get; private set; } = "sol";

    public int Fuel { get; private set; } = DefaultFuelCapacity;

    public int FuelCapacity => DefaultFuelCapacity;

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