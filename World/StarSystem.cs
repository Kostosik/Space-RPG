using GameProject.Navigation;

namespace GameProject.World;

public sealed class StarSystem
{
    private const double NavigationClearance = 10.0;

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

            obstacles.Add(
                new NavigationCircleObstacle(
                    new NavigationVector2(obj.X, obj.Y),
                    obj.VisualRadius + NavigationClearance));
        }

        NavigationObstacles =
            Array.AsReadOnly(obstacles.ToArray());
    }

    public StarObject Star { get; }

    public IReadOnlyList<StarSystemObject> Objects { get; }

    public IReadOnlyList<NavigationCircleObstacle>
        NavigationObstacles
    { get; }
}