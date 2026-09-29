namespace ShapesCli;

public record RGB(byte R, byte G, byte B)
{
    public static RGB Black { get; } = new(0, 0, 0);

    public override string ToString() => $"Color({R},{G},{B})";
}

public abstract class Shape
{
    public virtual RGB? Color => null;

    public abstract override string ToString();
    protected static string Fmt(double v) =>
        v.ToString(System.Globalization.CultureInfo.InvariantCulture);
}

public sealed class Point : Shape
{
    public double X { get; }
    public double Y { get; }

    public Point(double x, double y)
    {
        X = x;
        Y = y;
    }

    public override string ToString() =>
        $"Point({Fmt(X)}, {Fmt(Y)})";
}

public class Line : Shape
{
    public Point P1 { get; }
    public Point P2 { get; }
    public override RGB Color { get; }

    public Line(Point p1, Point p2, RGB color)
    {
        P1 = p1;
        P2 = p2;
        Color = color;
    }

    public override string ToString() => $"Line({P1}, {P2}, {Color})";
}

public class Circle : Shape
{
    public Point Center { get; }
    public double Radius { get; }
    public override RGB Color { get; }

    public Circle(Point center, double radius, RGB color)
    {
        Center = center;
        Radius = radius;
        Color = color;
    }

    public override string ToString() =>
        $"Circle({Center}, {Fmt(Radius)}, {Color})";
}