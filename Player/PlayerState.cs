using GameProject.Ships;

namespace GameProject.Player;

/// <summary>
/// Persistent state owned by the player.
/// Does not contain UI or engine runtime objects.
/// </summary>
public sealed class PlayerState
{
    public PlayerState(long initialCredits = 0)
    {
        if (initialCredits < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(initialCredits));
        }

        Credits = initialCredits;
    }

    public GalaxyNavigationState GalaxyNavigation { get; } = new();

    public PlayerShipState Ship { get; } = new("starter_scout");

    public long Credits { get; private set; }

    internal bool TrySpendCredits(long amount)
    {
        if (amount < 0 || Credits < amount)
            return false;

        Credits -= amount;
        return true;
    }

    internal bool CanReceiveCredits(long amount)
    {
        return amount >= 0 &&
               Credits <= long.MaxValue - amount;
    }

    internal bool TryAddCredits(long amount)
    {
        if (!CanReceiveCredits(amount))
            return false;

        Credits += amount;
        return true;
    }
}