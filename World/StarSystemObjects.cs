namespace GameProject.World;

public abstract class StarSystemObject
{
    protected StarSystemObject(
        string name,
        double x,
        double y,
        double visualRadius)
    {
        Name = name;
        X = x;
        Y = y;
        VisualRadius = visualRadius;
    }

    public string Name { get; }
    public double X { get; }
    public double Y { get; }
    public double VisualRadius { get; }
}

public sealed class StarObject : StarSystemObject
{
    public StarObject(
        string name,
        double x,
        double y,
        double visualRadius,
        double navigationRadius)
        : base(name, x, y, visualRadius)
    {
        NavigationRadius = navigationRadius;
    }

    public double NavigationRadius { get; }
}

public sealed class PlanetObject : StarSystemObject
{
    public PlanetObject(
        string name,
        double x,
        double y,
        double radius,
        string description)
        : base(name, x, y, radius)
    {
        Description = description;
    }

    public string Description { get; }
}

public sealed class AsteroidObject : StarSystemObject
{
    public AsteroidObject(
        string name,
        double x,
        double y,
        double radius)
        : base(name, x, y, radius)
    {
    }
}

public sealed class StationObject : StarSystemObject
{
    public StationObject(
        string name,
        double x,
        double y,
        double radius,
        string description)
        : base(name, x, y, radius)
    {
        Description = description;
    }

    public string Description { get; }
}