
namespace GameProject.Navigation;

public sealed class NavigationPath
{
    private readonly NavigationVector2[] _points;

    internal NavigationPath(
        IEnumerable<NavigationVector2> points)
    {
        _points = points.ToArray();

        if (_points.Length < 2)
            throw new ArgumentException("Path requires at least two points.");

        Length = 0;

        for (var i = 1; i < _points.Length; i++)
            Length += NavigationVector2.Distance(
                _points[i - 1], _points[i]);
    }

    public IReadOnlyList<NavigationVector2> Points => _points;

    public double Length { get; }
}
