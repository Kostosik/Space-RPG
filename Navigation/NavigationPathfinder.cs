
namespace GameProject.Navigation;

public sealed class NavigationPathfinder
{
    private const double Epsilon = 1e-9;

    private readonly int _obstacleResolution;

    public NavigationPathfinder(int obstacleResolution = 32)
    {
        if (obstacleResolution < 8)
            throw new ArgumentOutOfRangeException(
                nameof(obstacleResolution));

        _obstacleResolution = obstacleResolution;
    }

    public NavigationPath? FindPath(
        NavigationVector2 start,
        NavigationVector2 goal,
        IReadOnlyList<NavigationCircleObstacle> obstacles)
    {
        ArgumentNullException.ThrowIfNull(obstacles);

        if (!start.IsFinite || !goal.IsFinite)
            throw new ArgumentException("Coordinates must be finite.");

        if (NavigationVector2.Distance(start, goal) <= Epsilon)
            return new NavigationPath(new[] { start, goal });

        foreach (var obstacle in obstacles)
        {
            if (IsInside(start, obstacle) || IsInside(goal, obstacle))
                return null;
        }

        // Первым делом проверяем прямой маршрут.
        if (IsVisible(start, goal, obstacles))
            return new NavigationPath(new[] { start, goal });

        var nodes = new List<NavigationVector2>
        {
            start,
            goal
        };

        // Строим описанные многоугольники вокруг окружностей.
        foreach (var obstacle in obstacles)
        {
            var vertexRadius =
                obstacle.Radius /
                Math.Cos(Math.PI / _obstacleResolution);

            for (var i = 0; i < _obstacleResolution; i++)
            {
                var angle =
                    2.0 * Math.PI * i / _obstacleResolution;

                nodes.Add(new NavigationVector2(
                    obstacle.Center.X + Math.Cos(angle) * vertexRadius,
                    obstacle.Center.Y + Math.Sin(angle) * vertexRadius));
            }
        }

        var count = nodes.Count;
        var distances = new double[count];
        var previous = new int[count];
        var visited = new bool[count];

        Array.Fill(distances, double.PositiveInfinity);
        Array.Fill(previous, -1);

        distances[0] = 0;

        // Дейкстра на графе видимости.
        for (var iteration = 0; iteration < count; iteration++)
        {
            var current = -1;
            var bestDistance = double.PositiveInfinity;

            for (var i = 0; i < count; i++)
            {
                if (!visited[i] && distances[i] < bestDistance)
                {
                    current = i;
                    bestDistance = distances[i];
                }
            }

            if (current < 0)
                break;

            if (current == 1)
                break;

            visited[current] = true;

            for (var next = 0; next < count; next++)
            {
                if (visited[next] || next == current)
                    continue;

                if (!IsVisible(nodes[current], nodes[next], obstacles))
                    continue;

                var candidate =
                    distances[current] +
                    NavigationVector2.Distance(
                        nodes[current], nodes[next]);

                if (candidate < distances[next])
                {
                    distances[next] = candidate;
                    previous[next] = current;
                }
            }
        }

        if (!double.IsFinite(distances[1]))
            return null;

        var result = new List<NavigationVector2>();
        var index = 1;

        while (index >= 0)
        {
            result.Add(nodes[index]);

            if (index == 0)
                break;

            index = previous[index];
        }

        if (result[^1] != start)
            return null;

        result.Reverse();

        return new NavigationPath(RemoveCollinearPoints(result));
    }

    private static bool IsInside(
        NavigationVector2 point,
        NavigationCircleObstacle obstacle)
    {
        var distanceSquared =
            (point - obstacle.Center).LengthSquared;

        var radiusSquared =
            obstacle.Radius * obstacle.Radius;

        return distanceSquared < radiusSquared - Epsilon;
    }

    private static bool IsVisible(
        NavigationVector2 start,
        NavigationVector2 end,
        IReadOnlyList<NavigationCircleObstacle> obstacles)
    {
        foreach (var obstacle in obstacles)
        {
            if (SegmentIntersectsCircleInterior(
                    start, end, obstacle))
                return false;
        }

        return true;
    }

    private static bool SegmentIntersectsCircleInterior(
        NavigationVector2 start,
        NavigationVector2 end,
        NavigationCircleObstacle obstacle)
    {
        var segment = end - start;
        var lengthSquared = segment.LengthSquared;

        if (lengthSquared <= Epsilon)
            return IsInside(start, obstacle);

        var toCenter = obstacle.Center - start;

        var t = NavigationVector2.Dot(
            toCenter, segment) / lengthSquared;

        t = Math.Clamp(t, 0.0, 1.0);

        var closestPoint = start + segment * t;

        var distanceSquared =
            (closestPoint - obstacle.Center).LengthSquared;

        var radiusSquared =
            obstacle.Radius * obstacle.Radius;

        return distanceSquared < radiusSquared - Epsilon;
    }

    private static IEnumerable<NavigationVector2> RemoveCollinearPoints(
        IReadOnlyList<NavigationVector2> points)
    {
        if (points.Count <= 2)
            return points;

        var result = new List<NavigationVector2>
        {
            points[0]
        };

        for (var i = 1; i < points.Count - 1; i++)
        {
            var a = result[^1];
            var b = points[i];
            var c = points[i + 1];

            var ab = b - a;
            var bc = c - b;

            var cross = ab.X * bc.Y - ab.Y * bc.X;

            if (Math.Abs(cross) > Epsilon)
                result.Add(b);
        }

        result.Add(points[^1]);

        return result;
    }
}
