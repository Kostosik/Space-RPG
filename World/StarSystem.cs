using GameProject.Navigation;
using System.Collections.Generic;
using System.Linq;

namespace GameProject.World;

public sealed class StarSystem
{
    public StarSystem(
        StarObject star,
        IReadOnlyList<StarSystemObject> objects)
    {
        Star = star;
        Objects = objects;

        NavigationObstacles = new[]
        {
            new NavigationCircleObstacle(
                new NavigationVector2(star.X, star.Y),
                star.NavigationRadius)
        };
    }

    public StarObject Star { get; }

    public IReadOnlyList<StarSystemObject> Objects { get; }

    public IReadOnlyList<NavigationCircleObstacle>
        NavigationObstacles
    { get; }
}