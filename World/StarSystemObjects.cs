
namespace GameProject.World;

public abstract class StarSystemObject
{
    protected StarSystemObject(
        string id,
        string name,
        double x,
        double y,
        double visualRadius)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (!string.Equals(
                id,
                id.Trim(),
                StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "Object ID cannot contain surrounding whitespace.",
                nameof(id));
        }

        Id = id;
        Name = name;
        X = x;
        Y = y;
        VisualRadius = visualRadius;
    }

    public string Id { get; }

    public string Name { get; }

    public double X { get; private set; }

    public double Y { get; private set; }

    protected void SetPosition(double x, double y)
    {
        X = x;
        Y = y;
    }

    public double VisualRadius { get; }
}

public sealed class StarObject : StarSystemObject
{
    public StarObject(
        string id,
        string name,
        double x,
        double y,
        double visualRadius,
        double navigationRadius)
        : base(id, name, x, y, visualRadius)
    {
        NavigationRadius = navigationRadius;
    }

    public double NavigationRadius { get; }
}

public sealed class PlanetObject : StarSystemObject
{
    public PlanetObject(
        string id,
        string name,
        double x,
        double y,
        double radius,
        string description)
        : this(
            id,
            name,
            Math.Sqrt(x * x + y * y),
            90.0,
            Math.Atan2(y, x),
            radius,
            description)
    {
    }

    public PlanetObject(
        string id,
        string name,
        double orbitRadius,
        double orbitalPeriodDays,
        double initialAngleRadians,
        double radius,
        string description,
        double centerX = 0.0,
        double centerY = 0.0)
        : base(
            id,
            name,
            centerX + Math.Cos(initialAngleRadians) * orbitRadius,
            centerY + Math.Sin(initialAngleRadians) * orbitRadius,
            radius)
    {
        if (!double.IsFinite(orbitRadius) || orbitRadius <= 0)
            throw new ArgumentOutOfRangeException(nameof(orbitRadius));

        if (!double.IsFinite(orbitalPeriodDays) || orbitalPeriodDays <= 0)
            throw new ArgumentOutOfRangeException(nameof(orbitalPeriodDays));

        OrbitRadius = orbitRadius;
        OrbitalPeriodDays = orbitalPeriodDays;
        OrbitAngleRadians = initialAngleRadians;
        Description = description;
    }

    public string Description { get; }

    public double OrbitRadius { get; }

    public double OrbitalPeriodDays { get; }

    public double OrbitAngleRadians { get; private set; }

    public void AdvanceOrbit(
        double elapsedGameDays,
        double centerX,
        double centerY)
    {
        if (!double.IsFinite(elapsedGameDays) || elapsedGameDays <= 0)
            return;

        OrbitAngleRadians =
            (OrbitAngleRadians +
             Math.Tau * elapsedGameDays / OrbitalPeriodDays)
            % Math.Tau;

        var x = centerX +
                Math.Cos(OrbitAngleRadians) * OrbitRadius;

        var y = centerY +
                Math.Sin(OrbitAngleRadians) * OrbitRadius;

        SetPosition(x, y);
    }
}

public sealed class AsteroidObject : StarSystemObject
{
    public AsteroidObject(
        string id,
        string name,
        double x,
        double y,
        double radius)
        : base(id, name, x, y, radius)
    {
    }
}

public sealed class StationObject : StarSystemObject
{
    public StationObject(
        string id,
        string name,
        double x,
        double y,
        double radius,
        string description)
        : base(id, name, x, y, radius)
    {
        Description = description;
    }

    public string Description { get; }
}
