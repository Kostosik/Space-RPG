using GameProject.Navigation;

namespace GameProject.World;

public sealed class StarSystem
{
    public StarSystem(
        StarObject star,
        IReadOnlyList<StarSystemObject> objects)
    {
        ArgumentNullException.ThrowIfNull(star);
        ArgumentNullException.ThrowIfNull(objects);

        if (objects.Any(obj => obj is null))
        {
            throw new ArgumentException(
                "Star system cannot contain null objects.",
                nameof(objects));
        }

        Star = star;
        Objects = Array.AsReadOnly(objects.ToArray());

        // Единственное навигационное препятствие — центральная звезда.
        NavigationObstacles = Array.AsReadOnly(
            new[]
            {
                new NavigationCircleObstacle(
                    new NavigationVector2(star.X, star.Y),
                    star.NavigationRadius)
            });
    }

    public void AdvanceOrbit(double elapsedGameDays)
    {
        if (!double.IsFinite(elapsedGameDays) ||
            elapsedGameDays <= 0)
        {
            return;
        }

        // Планеты продолжают двигаться по орбитам.
        // Они не являются навигационными препятствиями.
        foreach (var planet in Objects.OfType<PlanetObject>())
        {
            planet.AdvanceOrbit(
                elapsedGameDays,
                Star.X,
                Star.Y);
        }
    }

    public StarObject Star { get; }

    public IReadOnlyList<StarSystemObject> Objects { get; }

    public IReadOnlyList<NavigationCircleObstacle>
        NavigationObstacles
    { get; }
}