using System.CommandLine;
using System.Globalization;

namespace ShapesCli;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        // Опции

        var fileOption = new Option<FileInfo>("-f", "--file")
        {
            Required = true,
            Description = "Путь к файлу с описаниями объектов"
        };

        var operOption = new Option<string>("-o", "--oper")
        {
            Required = true,
            Description = "Операция над списком фигур: print | count | del"
        };
        operOption.Validators.Add(result =>
        {
            var value = result.GetValueOrDefault<string>();
            if (value is not ("print" or "count" or "del"))
                result.AddError("Допустимые значения: print, count, del");
        });

        var propOption = new Option<string?>("--prop")
        {
            Description = "Свойство для --oper del, например: color=RGB(255,127,15). " +
                          "Если не задано — список очищается полностью."
        };

        // Команда

        var root = new RootCommand(
            "Чтение и обработка геометрических объектов из файла");
        root.Options.Add(fileOption);
        root.Options.Add(operOption);
        root.Options.Add(propOption);

        // Действие

        root.SetAction(async (parseResult) =>
        {
            var file = parseResult.GetValue(fileOption)!;
            var oper = parseResult.GetValue(operOption)!;
            var prop = parseResult.GetValue(propOption);

            if (prop is not null && oper != "del")
            {
                Console.Error.WriteLine(
                    "[warning] --prop имеет смысл только с --oper del, игнорируется");
                prop = null;
            }

            List<Shape> shapes;
            try
            {
                shapes = ShapeReader.ReadShapes(file.FullName);
            }
            catch (FileNotFoundException)
            {
                Console.Error.WriteLine($"[error] файл не найден: {file.FullName}");
                return 1;
            }
            catch (IOException ex)
            {
                Console.Error.WriteLine($"[error] не удалось прочитать файл: {ex.Message}");
                return 1;
            }

            switch (oper)
            {
                case "print":
                    foreach (var s in shapes)
                        Console.WriteLine(s);
                    break;

                case "count":
                    Console.WriteLine(shapes.Count);
                    break;

                case "del":
                    var (ok, removed) = DeleteByProp(shapes, prop);
                    if (!ok) return 2;
                    Console.WriteLine($"Удалено: {removed}");
                    break;
            }

            return 0;
        });

        var parseResult = root.Parse(args);
        return await parseResult.InvokeAsync();
    }

    private static (bool Ok, int Removed) DeleteByProp(List<Shape> shapes, string? prop)
    {
        if (prop is null)
        {
            var n = shapes.Count;
            shapes.Clear();
            return (true, n);
        }

        var parts = prop.Split('=', 2);
        if (parts.Length != 2 || parts[0].Trim().Length == 0)
        {
            Console.Error.WriteLine(
                "[error] неверный формат --prop, ожидается key=value");
            return (false, 0);
        }

        var key = parts[0].Trim();
        var value = parts[1].Trim();

        if (!key.Equals("color", StringComparison.OrdinalIgnoreCase))
        {
            Console.Error.WriteLine($"[warning] неизвестное свойство: {key}");
            return (true, 0);
        }

        if (!TryParseRgb(value, out var target))
        {
            Console.Error.WriteLine(
                "[error] значение color должно иметь вид RGB(r,g,b), r,g,b принадлежат [0,255]");
            return (false, 0);
        }

        return (true, shapes.RemoveAll(s => target.Equals(s.Color)));
    }

    private static bool TryParseRgb(string value, out RGB rgb)
    {
        rgb = RGB.Black;

        if (!value.StartsWith("RGB(", StringComparison.OrdinalIgnoreCase) ||
            !value.EndsWith(")"))
        {
            return false;
        }

        var inner = value[4..^1];
        var parts = inner.Split(',');
        if (parts.Length != 3) return false;

        if (!byte.TryParse(parts[0].Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var r) ||
            !byte.TryParse(parts[1].Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var g) ||
            !byte.TryParse(parts[2].Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var b))
        {
            return false;
        }

        rgb = new RGB(r, g, b);
        return true;
    }
}