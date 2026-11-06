namespace GameProject.Navigation;

public sealed class NavigationPath
{
    private readonly NavigationVector2[] _points;
    private readonly IReadOnlyList<NavigationVector2> _readOnlyPoints;

    internal NavigationPath(
        IEnumerable<NavigationVector2> points)
    {
        ArgumentNullException.ThrowIfNull(points);

        _points = points.ToArray();

        if (_points.Length < 2)
        {
            throw new ArgumentException(
                "Path requires at least two points.",
                nameof(points));
        }

        Length = 0.0;

        for (var i = 0; i < _points.Length; i++)
        {
            if (!_points[i].IsFinite)
            {
                throw new ArgumentException(
                    "Path points must have finite coordinates.",
                    nameof(points));
            }

            if (i == 0)
                continue;

            var segmentLength = NavigationVector2.Distance(
                _points[i - 1],
                _points[i]);

            if (!double.IsFinite(segmentLength))
            {
                throw new ArgumentException(
                    "Path length must be finite.",
                    nameof(points));
            }

            Length += segmentLength;
        }

        if (!double.IsFinite(Length))
        {
            throw new ArgumentException(
                "Total path length must be finite.",
                nameof(points));
        }

        // Наружу отдаём только read-only представление.
        _readOnlyPoints = Array.AsReadOnly(_points);
    }

    public IReadOnlyList<NavigationVector2> Points =>
        _readOnlyPoints;

    public double Length { get; }
}