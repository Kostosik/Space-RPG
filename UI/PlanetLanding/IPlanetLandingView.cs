using Engine.UI.Core;

namespace GameProject.UI.PlanetLanding;

public interface IPlanetLandingView
{
    string Id { get; }

    string Title { get; }

    UiWidget Content { get; }
}