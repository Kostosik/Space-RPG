using GameProject.Navigation;

namespace GameProject.World;

public sealed class StarSystem
{
    public const double DefaultJumpBoundaryClearance = 24.0;
    public const double DefaultJumpBoundaryRadius = 700.0;
    public const double DefaultArrivalBoundaryOffset = 80.0;
    public StarSystem(
        StarObject star,
        IReadOnlyList<StarSystemObject> objects, double jumpBoundaryRadius = DefaultJumpBoundaryRadius)
    {
        ArgumentNullException.ThrowIfNull(star);
        ArgumentNullException.ThrowIfNull(objects);

        if (objects.Any(obj => obj is null))
        {
            throw new ArgumentException(
                "Star system cannot contain null objects.",
                nameof(objects));
        }
        if (!double.IsFinite(jumpBoundaryRadius) ||
    jumpBoundaryRadius <= star.NavigationRadius)
        {
            throw new ArgumentOutOfRangeException(
                nameof(jumpBoundaryRadius));
        }
        Star = star;
        JumpBoundaryRadius = jumpBoundaryRadius;
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

    public double JumpBoundaryRadius { get; }

    public bool IsWithinJumpBoundary(
        NavigationVector2 position,
        double clearance = 0.0)
    {
        if (!position.IsFinite)
            throw new ArgumentOutOfRangeException(nameof(position));

        var usableRadius = GetUsableBoundaryRadius(clearance);

        var center = new NavigationVector2(Star.X, Star.Y);

        return NavigationVector2.Distance(position, center)
            <= usableRadius;
    }

    public NavigationVector2 ClampToJumpBoundary(
        NavigationVector2 position,
        double clearance = 0.0)
    {
        if (!position.IsFinite)
            throw new ArgumentOutOfRangeException(nameof(position));

        var usableRadius = GetUsableBoundaryRadius(clearance);

        var center = new NavigationVector2(Star.X, Star.Y);
        var offset = position - center;
        var distance = offset.Length;

        if (distance <= usableRadius || distance <= 0.000001)
            return position;

        return center + offset * (usableRadius / distance);
    }

    private double GetUsableBoundaryRadius(double clearance)
    {
        if (!double.IsFinite(clearance) ||
            clearance < 0.0 ||
            clearance >= JumpBoundaryRadius)
        {
            throw new ArgumentOutOfRangeException(nameof(clearance));
        }

        return JumpBoundaryRadius - clearance;
    }

    public IReadOnlyList<StarSystemObject> Objects { get; }

    public IReadOnlyList<NavigationCircleObstacle>
        NavigationObstacles
    { get; }
}