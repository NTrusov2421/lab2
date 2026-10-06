namespace ShapesCli;

public static class ShapeReader
{
    // Читает файл построчно. Некорректные строки пропускает, отправляя warning в логгер.
    public static List<Shape> ReadShapes(string path)
    {
        var shapes = new List<Shape>();
        var lineNo = 0;

        foreach (var line in File.ReadLines(path))
        {
            lineNo++;
            var shape = ShapeParser.Parse(line);
            if (shape is not null)
            {
                shapes.Add(shape);
                continue;
            }

            if (!string.IsNullOrWhiteSpace(line))
                ErrorLogger.Warn($"[warning] строка {lineNo}: некорректное описание, пропущено");
        }

        return shapes;
    }
}