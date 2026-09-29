namespace ShapesCli;

public static class ShapeReader
{
    // Читает файл построчно, пропуская некорректные строки

    public static List<Shape> ReadShapes(string path)
    {
        var shapes = new List<Shape>();
        var lineNo = 0;

        foreach (var raw in File.ReadLines(path))
        {
            lineNo++;
            var shape = ShapeParser.Parse(raw);
            if (shape is null)
            {
                if (!string.IsNullOrWhiteSpace(raw))
                {
                    Console.Error.WriteLine($"[warning] строка {lineNo}: некорректное описание, пропущено");
                }
                continue;
            }
            shapes.Add(shape);
        }

        return shapes;
    }
}