using System.CommandLine;
using System.Text.RegularExpressions;

namespace ShapesCli;

public static class Program
{
    private static readonly Regex RgbRegex = new(
        @"^RGB\(\s*(\d{1,3})\s*,\s*(\d{1,3})\s*,\s*(\d{1,3})\s*\)$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static int Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.InputEncoding = System.Text.Encoding.UTF8;
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
        }.AcceptOnlyFromAmong("print", "count", "del");

        var propOption = new Option<string?>("--prop")
        {
            Description = "Свойство для --oper del в формате key=value"
        };

        // --log c необязательным значением:
        //   --log path.txt писать в path.txt и запомнить путь;
        //   --log писать в последний запомненный путь;
        //   без --log писать в переменные окружения.
        var logOption = new Option<string?>("--log")
        {
            Description = "Файл лога; без значения используется последний путь",
            Arity = ArgumentArity.ZeroOrOne
        };

        // Команда

        var root = new RootCommand(
            "Чтение и обработка геометрических объектов из файла");
        root.Options.Add(fileOption);
        root.Options.Add(operOption);
        root.Options.Add(propOption);
        root.Options.Add(logOption);

        // Действие

        root.SetAction(r => Execute(
            r.GetValue(fileOption)!,
            r.GetValue(operOption)!,
            r.GetValue(propOption),
            GetLogPath(r, logOption)));

        return root.Parse(args).Invoke();
    }

    private static int Execute(FileInfo file, string oper, string? prop, string? logPath)
    {
        if (prop is not null && oper != "del")
        {
            ErrorLogger.Warn("[warning] --prop имеет смысл только с --oper del, игнорируется");
            prop = null;
        }

        var exitCode = 0;
        try
        {
            var shapes = ShapeReader.ReadShapes(file.FullName);

            switch (oper)
            {
                case "print":
                    foreach (var shape in shapes) Console.WriteLine(shape);
                    break;

                case "count":
                    Console.WriteLine(shapes.Count);
                    break;

                case "del":
                    var removed = Delete(shapes, prop);
                    if (removed is null)
                        exitCode = 2;
                    else
                        Console.WriteLine($"Удалено: {removed}");
                    break;
            }
        }
        catch (IOException ex)
        {
            ErrorLogger.Error($"[error] не удалось прочитать файл: {ex.Message}");
            exitCode = 1;
        }

        ErrorLogger.Save(logPath);
        return exitCode;
    }

    private static string? GetLogPath(ParseResult result, Option<string?> option)
    {
        if (result.GetResult(option) is null) return null;

        var path = result.GetValue(option);
        if (!string.IsNullOrWhiteSpace(path))
        {
            LastLogState.Write(path);
            return path;
        }

        return LastLogState.Read();
    }

    private static int? Delete(List<Shape> shapes, string? prop)
    {
        if (prop is null)
        {
            var removed = shapes.Count;
            shapes.Clear();
            return removed;
        }

        var parts = prop.Split('=', 2);
        if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0]))
        {
            ErrorLogger.Error("[error] неверный формат --prop, ожидается key=value");
            return null;
        }

        var key = parts[0].Trim();
        var value = parts[1].Trim();
        if (!key.Equals("color", StringComparison.OrdinalIgnoreCase))
        {
            ErrorLogger.Warn($"[warning] неизвестное свойство: {key}");
            return 0;
        }

        if (!TryParseRgb(value, out var target))
        {
            ErrorLogger.Error("[error] значение color должно иметь вид RGB(r,g,b), r,g,b = [0,255]");
            return null;
        }

        return shapes.RemoveAll(s => s.Color == target);
    }

    private static bool TryParseRgb(string value, out RGB rgb)
    {
        rgb = RGB.Black;
        var m = RgbRegex.Match(value);
        if (!m.Success ||
            !byte.TryParse(m.Groups[1].Value, out var r) ||
            !byte.TryParse(m.Groups[2].Value, out var g) ||
            !byte.TryParse(m.Groups[3].Value, out var b))
            return false;

        rgb = new RGB(r, g, b);
        return true;
    }
}