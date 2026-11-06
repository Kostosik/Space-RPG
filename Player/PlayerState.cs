namespace GameProject.Player;

/// <summary>
/// Persistent state owned by the player.
/// Does not contain UI or engine runtime objects.
/// </summary>
public sealed class PlayerState
{
    public GalaxyNavigationState GalaxyNavigation { get; } = new();

    public PlayerShipState Ship { get; } = new("starter_scout");
}