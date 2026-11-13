using GameProject.World;

namespace GameProject.UI.PlanetLanding;

public sealed class PlanetLandingPresenter
{
    public PlanetLandingViewData CreateViewData(
        StarSystemObject landedObject)
    {
        ArgumentNullException.ThrowIfNull(landedObject);

        var (locationType, description) = landedObject switch
        {
            PlanetObject planet =>
                ("PLANET", planet.Description),

            StationObject station =>
                ("SPACE STATION", station.Description),

            _ =>
                ("UNKNOWN LOCATION", "No information available.")
        };

        if (string.IsNullOrWhiteSpace(description))
        {
            description = "No information available.";
        }

        return new PlanetLandingViewData(
            landedObject.Id,
            landedObject.Name,
            locationType,
            description);
    }
}