
namespace GameProject.Navigation;

public static class NavigationPathSmoother
{
    private const double Epsilon = 0.000001;

    // Максимальный угол между соседними точками дуги.
    private const double MaxArcStepRadians = Math.PI / 90.0;
    private const double InitialTurnToleranceRadians = Math.PI / 2.0;
    public static NavigationPath? Smooth(
        NavigationPath source,
        float startRotation,
        double turnRadius,
        IReadOnlyList<NavigationCircleObstacle> obstacles,
        NavigationPathfinder pathfinder)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(obstacles);

        if (!float.IsFinite(startRotation))
        {
            throw new ArgumentOutOfRangeException(
                nameof(startRotation));
        }

        if (!double.IsFinite(turnRadius) || turnRadius <= 0.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(turnRadius));
        }

        var start = source.Points[0];

        var firstDirection = Normalize(
            source.Points[1] - start);

        var heading = new NavigationVector2(
            Math.Cos(startRotation),
            Math.Sin(startRotation));

        var initialAngle = SignedAngle(
            heading,
            firstDirection);

        // Для почти полного разворота используем настоящую дугу,
        // а не вырождающуюся в прямую кривую Безье.
        if (Math.Abs(initialAngle) >= Math.PI * 0.85)
        {
            return BuildReverseTurn(
                source,
                startRotation,
                turnRadius,
                obstacles,
                pathfinder);
        }

        // При сложной геометрии пробуем постепенно
        // уменьшать радиус, сохраняя безопасный маршрут.
        double[] radiusFactors = [1.0, 0.75, 0.5];

        foreach (var factor in radiusFactors)
        {
            var radius = turnRadius * factor;

            // Сначала пробуем учесть начальный угол корабля.
            var points = BuildPoints(
                source.Points,
                startRotation,
                radius,
                includeInitialTurn: true);

            if (IsPathClear(points, obstacles))
                return new NavigationPath(points);

            // Если начальная дуга задевает препятствие,
            // сохраняем безопасную начальную прямую.
            points = BuildPoints(
                source.Points,
                startRotation,
                radius,
                includeInitialTurn: false);

            if (IsPathClear(points, obstacles))
                return new NavigationPath(points);
        }

        return null;
    }


    private static NavigationPath? BuildReverseTurn(
        NavigationPath source,
        float startRotation,
        double turnRadius,
        IReadOnlyList<NavigationCircleObstacle> obstacles,
        NavigationPathfinder pathfinder)
    {
        var start = source.Points[0];
        var goal = source.Points[^1];

        var heading = new NavigationVector2(
            Math.Cos(startRotation),
            Math.Sin(startRotation));

        var direction = Normalize(
            source.Points[1] - start);

        var angle = SignedAngle(heading, direction);

        // Проверяем оба направления разворота.
        var angles = new[]
        {
        angle,
        angle > 0.0
            ? angle - Math.Tau
            : angle + Math.Tau
    };

        double[] radiusFactors = [1.0, 0.75, 0.5];

        NavigationPath? bestPath = null;

        foreach (var factor in radiusFactors)
        {
            var radius = turnRadius * factor;

            foreach (var turnAngle in angles)
            {
                var arc = BuildCircularArc(
                    start,
                    heading,
                    radius,
                    turnAngle);

                if (!IsPathClear(arc, obstacles))
                    continue;

                var arcEnd = arc[^1];

                // После разворота заново строим путь
                // к первоначальной цели.
                var tailRaw = pathfinder.FindPath(
                    arcEnd,
                    goal,
                    obstacles);

                if (tailRaw is null)
                    continue;

                var finalHeading = Rotate(
                    heading,
                    turnAngle);

                var tailRotation = (float)Math.Atan2(
                    finalHeading.Y,
                    finalHeading.X);

                var tail = BuildPoints(
                    tailRaw.Points,
                    tailRotation,
                    radius,
                    includeInitialTurn: true);

                var combined = new List<NavigationVector2>(arc);

                for (var i = 1; i < tail.Count; i++)
                {
                    Append(combined, tail[i]);
                }

                if (combined.Count < 2 ||
                    !IsPathClear(combined, obstacles))
                {
                    continue;
                }

                var candidate = new NavigationPath(combined);

                if (bestPath is null ||
                    candidate.Length < bestPath.Length)
                {
                    bestPath = candidate;
                }
            }
        }

        return bestPath;
    }

    private static List<NavigationVector2> BuildCircularArc(
        NavigationVector2 start,
        NavigationVector2 heading,
        double radius,
        double angle)
    {
        var sign = Math.Sign(angle);

        var normal = new NavigationVector2(
            -heading.Y,
            heading.X);

        var center =
            start + normal * (sign * radius);

        var radial = start - center;

        var sampleCount = Math.Max(
            2,
            (int)Math.Ceiling(
                Math.Abs(angle) / MaxArcStepRadians));

        var points = new List<NavigationVector2>(
            sampleCount + 1);

        for (var i = 0; i <= sampleCount; i++)
        {
            var progress = (double)i / sampleCount;
            var currentAngle = angle * progress;

            var cos = Math.Cos(currentAngle);
            var sin = Math.Sin(currentAngle);

            var rotated = new NavigationVector2(
                radial.X * cos - radial.Y * sin,
                radial.X * sin + radial.Y * cos);

            points.Add(center + rotated);
        }

        return points;
    }

    private static NavigationVector2 Rotate(
        NavigationVector2 vector,
        double angle)
    {
        var cos = Math.Cos(angle);
        var sin = Math.Sin(angle);

        return new NavigationVector2(
            vector.X * cos - vector.Y * sin,
            vector.X * sin + vector.Y * cos);
    }


    private static List<NavigationVector2> BuildPoints(
        IReadOnlyList<NavigationVector2> source,
        float startRotation,
        double turnRadius,
        bool includeInitialTurn)
    {
        var result = new List<NavigationVector2>();

        var start = source[0];
        var currentPosition = start;

        Append(result, start);

        var firstDirection = Normalize(
            source[1] - start);

        var firstSegmentLength =
            NavigationVector2.Distance(start, source[1]);

        // Плавно соединяем текущее направление корабля
        // с первым сегментом маршрута.
        if (includeInitialTurn &&
            firstSegmentLength > Epsilon)
        {
            var heading = new NavigationVector2(
                Math.Cos(startRotation),
                Math.Sin(startRotation));

            var angle = SignedAngle(
                heading,
                firstDirection);

            if (Math.Abs(angle) > InitialTurnToleranceRadians)
            {
                var leadDistance = Math.Min(
                    firstSegmentLength * 0.65,
                    turnRadius * Math.Abs(angle) * 1.5);

                var handleLength = Math.Min(
                    leadDistance * 0.45,
                    (4.0 / 3.0) *
                    turnRadius *
                    Math.Tan(Math.Abs(angle) / 4.0));

                if (leadDistance > Epsilon &&
                    handleLength > Epsilon)
                {
                    var end =
                        start + firstDirection * leadDistance;

                    var control1 =
                        start + heading * handleLength;

                    var control2 =
                        end - firstDirection * handleLength;

                    var sampleCount = Math.Max(
                        4,
                        (int)Math.Ceiling(
                            Math.Abs(angle) /
                            MaxArcStepRadians));

                    for (var i = 1; i <= sampleCount; i++)
                    {
                        var t = (double)i / sampleCount;

                        Append(
                            result,
                            EvaluateBezier(
                                start,
                                control1,
                                control2,
                                end,
                                t));
                    }

                    currentPosition = end;
                }
            }
        }

        // Сглаживаем все последующие повороты маршрута.
        for (var i = 1; i < source.Count - 1; i++)
        {
            var corner = source[i];
            var next = source[i + 1];

            var incomingVector =
                corner - currentPosition;

            var outgoingVector =
                next - corner;

            var incomingLength = incomingVector.Length;
            var outgoingLength = outgoingVector.Length;

            if (incomingLength <= Epsilon ||
                outgoingLength <= Epsilon)
            {
                Append(result, corner);
                currentPosition = corner;
                continue;
            }

            var incoming =
                incomingVector / incomingLength;

            var outgoing =
                outgoingVector / outgoingLength;

            var angle = SignedAngle(
                incoming,
                outgoing);

            // Прямой участок не требует сглаживания.
            if (Math.Abs(angle) < 0.01)
            {
                Append(result, corner);
                currentPosition = corner;
                continue;
            }

            var tangent = Math.Tan(
                Math.Abs(angle) * 0.5);

            if (!double.IsFinite(tangent) ||
                tangent <= Epsilon)
            {
                Append(result, corner);
                currentPosition = corner;
                continue;
            }

            // Расстояние от вершины до точек касания дуги.
            var desiredTrim = turnRadius * tangent;

            // Не позволяем дугам соседних поворотов
            // пересекаться друг с другом.
            var maximumTrim =
                Math.Min(incomingLength, outgoingLength) * 0.45;

            var trim = Math.Min(
                desiredTrim,
                maximumTrim);

            var actualRadius = trim / tangent;

            var entry =
                corner - incoming * trim;

            var exit =
                corner + outgoing * trim;

            Append(result, entry);

            var turnSign = Math.Sign(angle);

            var normal = new NavigationVector2(
                -incoming.Y,
                incoming.X);

            var center =
                entry + normal * (turnSign * actualRadius);

            var radial = entry - center;

            var startAngle = Math.Atan2(
                radial.Y,
                radial.X);

            var sampleCount = Math.Max(
                1,
                (int)Math.Ceiling(
                    Math.Abs(angle) /
                    MaxArcStepRadians));

            // Строим дугу с постепенным изменением направления.
            for (var sample = 1;
                 sample <= sampleCount;
                 sample++)
            {
                var progress =
                    (double)sample / sampleCount;

                var currentAngle =
                    startAngle + angle * progress;

                var point = new NavigationVector2(
                    center.X +
                    Math.Cos(currentAngle) * actualRadius,

                    center.Y +
                    Math.Sin(currentAngle) * actualRadius);

                Append(result, point);
            }

            // Используем точную конечную точку дуги.
            Append(result, exit);

            currentPosition = exit;
        }

        Append(result, source[^1]);

        // NavigationPath требует минимум две точки.
        if (result.Count == 1)
            result.Add(source[^1]);

        return result;
    }

    private static NavigationVector2 EvaluateBezier(
        NavigationVector2 p0,
        NavigationVector2 p1,
        NavigationVector2 p2,
        NavigationVector2 p3,
        double t)
    {
        var u = 1.0 - t;

        return
            p0 * (u * u * u) +
            p1 * (3.0 * u * u * t) +
            p2 * (3.0 * u * t * t) +
            p3 * (t * t * t);
    }

    private static double SignedAngle(
        NavigationVector2 from,
        NavigationVector2 to)
    {
        var cross =
            from.X * to.Y - from.Y * to.X;

        var dot =
            NavigationVector2.Dot(from, to);

        return Math.Atan2(cross, dot);
    }

    private static NavigationVector2 Normalize(
        NavigationVector2 value)
    {
        var length = value.Length;

        if (length <= Epsilon)
            return NavigationVector2.Zero;

        return value / length;
    }

    private static void Append(
        List<NavigationVector2> points,
        NavigationVector2 point)
    {
        if (points.Count == 0 ||
            NavigationVector2.Distance(
                points[^1],
                point) > Epsilon)
        {
            points.Add(point);
        }
    }

    private static bool IsPathClear(
        IReadOnlyList<NavigationVector2> points,
        IReadOnlyList<NavigationCircleObstacle> obstacles)
    {
        for (var i = 0; i < points.Count - 1; i++)
        {
            foreach (var obstacle in obstacles)
            {
                if (SegmentIntersectsCircle(
                        points[i],
                        points[i + 1],
                        obstacle))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static bool SegmentIntersectsCircle(
        NavigationVector2 start,
        NavigationVector2 end,
        NavigationCircleObstacle obstacle)
    {
        var segment = end - start;
        var lengthSquared = segment.LengthSquared;

        if (lengthSquared <= Epsilon * Epsilon)
        {
            return NavigationVector2.Distance(
                start,
                obstacle.Center) < obstacle.Radius - Epsilon;
        }

        var toCenter = obstacle.Center - start;

        var t = NavigationVector2.Dot(
            toCenter,
            segment) / lengthSquared;

        t = Math.Clamp(t, 0.0, 1.0);

        var closest = start + segment * t;

        return NavigationVector2.Distance(
            closest,
            obstacle.Center) < obstacle.Radius - Epsilon;
    }
}
