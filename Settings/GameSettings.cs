
namespace GameProject.Settings;

public sealed class GameSettings
{
    public DisplaySettings Display { get; set; } = new();

    public AudioSettings Audio { get; set; } = new();

    public GameplaySettings Gameplay { get; set; } = new();

    /// <summary>
    /// Creates an independent copy for editing settings
    /// without immediately changing the active configuration.
    /// </summary>
    public GameSettings CreateCopy()
    {
        return new GameSettings
        {
            Display = Display.CreateCopy(),
            Audio = Audio.CreateCopy(),
            Gameplay = Gameplay.CreateCopy()
        };
    }

    /// <summary>
    /// Restores all settings to their default values.
    /// </summary>
    public void ResetToDefaults()
    {
        Display = new DisplaySettings();
        Audio = new AudioSettings();
        Gameplay = new GameplaySettings();
    }
}
