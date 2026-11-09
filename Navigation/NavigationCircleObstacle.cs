
namespace GameProject.Navigation;

public sealed class NavigationCircleObstacle
{
    public NavigationCircleObstacle(
        NavigationVector2 center,
        double radius)
    {
        if (!center.IsFinite)
        {
            throw new ArgumentException(
                "Center must be finite.",
                nameof(center));
        }

        if (!double.IsFinite(radius) || radius <= 0.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(radius));
        }

        Center = center;
        Radius = radius;
    }

    public NavigationVector2 Center { get; }

    public double Radius { get; }
}
