using GameProject.Navigation;

namespace GameProject.World;

public sealed class StarSystem
{
    private const double NavigationClearance = 10.0;
    private readonly Dictionary<PlanetObject, NavigationCircleObstacle>
    _planetObstacles = new();
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

        var obstacles = new List<NavigationCircleObstacle>
        {
            new(
                new NavigationVector2(star.X, star.Y),
                star.NavigationRadius)
        };

        foreach (var obj in Objects)
        {
            // Звезда уже добавлена отдельно:
            // у неё свой навигационный радиус.
            if (obj is StarObject)
                continue;

            var obstacle = new NavigationCircleObstacle(
                new NavigationVector2(obj.X, obj.Y),
                obj.VisualRadius + NavigationClearance);

            obstacles.Add(obstacle);

            if (obj is PlanetObject planet)
            {
                _planetObstacles.Add(planet, obstacle);
            }
        }

        NavigationObstacles =
            Array.AsReadOnly(obstacles.ToArray());
    }

    public void AdvanceOrbit(double elapsedGameDays)
    {
        if (!double.IsFinite(elapsedGameDays) ||
            elapsedGameDays <= 0)
        {
            return;
        }

        foreach (var entry in _planetObstacles)
        {
            var planet = entry.Key;

            // Двигаем планету вокруг звезды.
            planet.AdvanceOrbit(
                elapsedGameDays,
                Star.X,
                Star.Y);

            // Обновляем положение навигационного препятствия.
            entry.Value.Center = new NavigationVector2(
                planet.X,
                planet.Y);
        }
    }

    public StarObject Star { get; }

    public IReadOnlyList<StarSystemObject> Objects { get; }

    public IReadOnlyList<NavigationCircleObstacle>
        NavigationObstacles
    { get; }
}