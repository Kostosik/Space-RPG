
namespace GameProject.Settings;

public sealed class GameplaySettings
{
    public bool ShowPlanetTooltips { get; set; } = true;

    public GameplaySettings CreateCopy()
    {
        return new GameplaySettings
        {
            ShowPlanetTooltips = ShowPlanetTooltips
        };
    }
}
