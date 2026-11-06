
namespace GameProject.Navigation;

public readonly record struct NavigationVector2(double X, double Y)
{
    public static NavigationVector2 Zero => new(0, 0);

    public static NavigationVector2 operator +(
        NavigationVector2 a, NavigationVector2 b)
        => new(a.X + b.X, a.Y + b.Y);

    public static NavigationVector2 operator -(
        NavigationVector2 a, NavigationVector2 b)
        => new(a.X - b.X, a.Y - b.Y);

    public static NavigationVector2 operator *(
        NavigationVector2 a, double value)
        => new(a.X * value, a.Y * value);

    public static NavigationVector2 operator /(
        NavigationVector2 a, double value)
        => new(a.X / value, a.Y / value);

    public double LengthSquared => X * X + Y * Y;

    public double Length => Math.Sqrt(LengthSquared);

    public static double Distance(
        NavigationVector2 a, NavigationVector2 b)
        => (a - b).Length;

    public static double Dot(
        NavigationVector2 a, NavigationVector2 b)
        => a.X * b.X + a.Y * b.Y;

    public bool IsFinite =>
        double.IsFinite(X) && double.IsFinite(Y);
}
