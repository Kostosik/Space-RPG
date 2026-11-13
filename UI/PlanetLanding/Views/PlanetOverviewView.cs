using Engine.Graphics.Commands;
using Engine.UI.Controls;
using Engine.UI.Core;
using Engine.UI.Layout;
using GameProject.UI.PlanetLanding;

namespace GameProject.UI.PlanetLanding.Views;

public sealed class PlanetOverviewView : IPlanetLandingView
{
    public const string ViewId = "overview";

    public string Id => ViewId;

    public string Title => "OVERVIEW";

    public UiWidget Content { get; }

    public PlanetOverviewView(
        PlanetLandingViewData data)
    {
        ArgumentNullException.ThrowIfNull(data);

        var content = new UiStackPanel
        {
            Orientation = UiOrientation.Vertical,
            Spacing = 16.0f
        };

        content.AddChild(new UiLabel("LOCATION OVERVIEW")
        {
            FontSize = 24.0f,
            Color = new UiColor(110, 210, 245, 255)
        });

        content.AddChild(new UiPanel
        {
            Height = 1.0f,
            Background = new UiColor(65, 105, 130, 255)
        });

        content.AddChild(new UiLabel("DESCRIPTION")
        {
            FontSize = 13.0f,
            Color = new UiColor(140, 175, 195, 255)
        });

        content.AddChild(new UiLabel(data.Description)
        {
            FontSize = 20.0f,
            Color = new UiColor(225, 235, 245, 255)
        });

        Content = content;
    }
}