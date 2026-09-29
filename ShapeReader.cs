namespace ShapesCli;

public static class ShapeReader
{
    // Читает файл построчно. Некорректные строки пропускает, отправляя warning в логгер.
    public static List<Shape> ReadShapes(string path, ErrorLogger logger)
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
                    logger.Warn(
                        $"[warning] строка {lineNo}: некорректное описание, пропущено");
                }
                continue;
            }
            shapes.Add(shape);
        }

        return shapes;
    }
}