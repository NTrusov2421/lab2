using System.Globalization;
using System.Text.RegularExpressions;

namespace ShapesCli;

//Разбор строк файла в объекты Shape
public static class ShapeParser
{
    private const string Num = @"-?\d+(?:\.\d+)?";

    private const string ColorSuffix =
        @"(?:,\s*Color\(\s*(?<r>\d{1,3})\s*,\s*(?<g>\d{1,3})\s*,\s*(?<b>\d{1,3})\s*\))?";

    private static readonly Regex LineRegex = new(
        $@"^Line\(\s*Point\(\s*(?<x1>{Num})\s*,\s*(?<y1>{Num})\s*\)\s*," +
        $@"\s*Point\(\s*(?<x2>{Num})\s*,\s*(?<y2>{Num})\s*\){ColorSuffix}\s*\)$",
        RegexOptions.Compiled);

    private static readonly Regex CircleRegex = new(
        $@"^Circle\(\s*Point\(\s*(?<x>{Num})\s*,\s*(?<y>{Num})\s*\)" +
        $@"\s*,\s*(?<rad>{Num}){ColorSuffix}\s*\)$",
        RegexOptions.Compiled);

    private static readonly Regex PointRegex = new(
        $@"^Point\(\s*(?<x>{Num})\s*,\s*(?<y>{Num})\s*\)$",
        RegexOptions.Compiled);

    // Разбирает строку.Возвращает Shape или null,
    // если строка не соответствует ни одному шаблону / цвет некорректен.
    public static Shape? Parse(string? line)
    {
        if (line is null) return null;
        line = line.Trim();
        if (line.Length == 0) return null;

        var m = LineRegex.Match(line);
        if (m.Success)
        {
            var color = ExtractColor(m);
            if (color is null) return null;
            return new Line(
                new Point(ParseDouble(m, "x1"), ParseDouble(m, "y1")),
                new Point(ParseDouble(m, "x2"), ParseDouble(m, "y2")),
                color);
        }

        m = CircleRegex.Match(line);
        if (m.Success)
        {
            var color = ExtractColor(m);
            if (color is null) return null;
            return new Circle(
                new Point(ParseDouble(m, "x"), ParseDouble(m, "y")),
                ParseDouble(m, "rad"),
                color);
        }

        m = PointRegex.Match(line);
        if (m.Success)
        {
            return new Point(ParseDouble(m, "x"), ParseDouble(m, "y"));
        }

        return null;
    }

    // цвет отсутствует в строке - RGB.Black.
    // цвет есть и валиден - RGB.
    // цвет есть, но вне [0,255] - null
    private static RGB? ExtractColor(Match m)
    {
        if (!m.Groups["r"].Success) return RGB.Black;

        if (byte.TryParse(m.Groups["r"].Value, out var r) &&
            byte.TryParse(m.Groups["g"].Value, out var g) &&
            byte.TryParse(m.Groups["b"].Value, out var b))
        {
            return new RGB(r, g, b);
        }
        return null;
    }

    private static double ParseDouble(Match m, string group) =>
        double.Parse(m.Groups[group].Value, CultureInfo.InvariantCulture);
}