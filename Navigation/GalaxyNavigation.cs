using GameProject.World;
public sealed class GalaxyNavigation
{
    public string CurrentSystemId { get; private set; }

    public int Fuel { get; private set; }

    public GalaxyNavigation(
        string startSystemId,
        int startingFuel)
    {
        CurrentSystemId = startSystemId;
        Fuel = startingFuel;
    }

    public bool TryJump(
        GalaxyMap galaxy,
        string targetSystemId,
        int fuelCost)
    {
        if (!galaxy.CanTravel(
                CurrentSystemId,
                targetSystemId))
        {
            return false;
        }

        if (fuelCost <= 0 || Fuel < fuelCost)
            return false;

        Fuel -= fuelCost;
        CurrentSystemId = targetSystemId;

        return true;
    }

    public void AddFuel(int amount)
    {
        if (amount > 0)
            Fuel += amount;
    }
}