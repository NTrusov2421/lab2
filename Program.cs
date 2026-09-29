using System.CommandLine;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ShapesCli;

public static class Program
{
    private static readonly Regex RgbRegex = new(
        @"^RGB\(\s*(\d{1,3})\s*,\s*(\d{1,3})\s*,\s*(\d{1,3})\s*\)$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

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

        // --log c необязательным значением:
        //   --log path.txt писать в path.txt и запомнить путь;
        //   --log писать в последний запомненный путь;
        //   без --log писать в переменные окружения.
        var logOption = new Option<string?>("--log")
        {
            Description = "Файл для сохранения ошибок. Без значения — в последний " +
                          "использованный файл. Если --log не указан — в переменные " +
                          "окружения SHAPESCLI_ERRORS / SHAPESCLI_ERROR_COUNT.",
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

        root.SetAction(async (parseResult) =>
        {
            var file = parseResult.GetValue(fileOption)!;
            var oper = parseResult.GetValue(operOption)!;
            var prop = parseResult.GetValue(propOption);

            var logger = new ErrorLogger();
            var logPath = ResolveLogPath(parseResult, logOption);

            if (prop is not null && oper != "del")
            {
                logger.Warn(
                    "[warning] --prop имеет смысл только с --oper del, игнорируется");
                prop = null;
            }

            int exitCode;
            try
            {
                var shapes = ShapeReader.ReadShapes(file.FullName, logger);
                exitCode = RunOperation(oper, shapes, prop, logger);
            }
            catch (IOException ex)
            {
                logger.Error($"[error] не удалось прочитать файл: {ex.Message}");
                exitCode = 1;
            }

            logger.Save(logPath);
            return exitCode;
        });

        var parseResult = root.Parse(args);
        return await parseResult.InvokeAsync();
    }

    // Определяет, куда сохранять лог
    private static string? ResolveLogPath(
        ParseResult parseResult, Option<string?> logOption)
    {
        if (parseResult.GetResult(logOption) is null)
            return null;

        var path = parseResult.GetValue(logOption);
        if (!string.IsNullOrWhiteSpace(path))
        {
            LastLogState.Write(path);
            return path;
        }

        return LastLogState.Read();
    }

    // Операции

    private static int RunOperation(
        string oper, List<Shape> shapes, string? prop, ErrorLogger logger)
    {
        switch (oper)
        {
            case "print":
                foreach (var s in shapes)
                    Console.WriteLine(s);
                return 0;

            case "count":
                Console.WriteLine(shapes.Count);
                return 0;

            case "del":
                var (ok, removed) = DeleteByProp(shapes, prop, logger);
                if (!ok) return 2;
                Console.WriteLine($"Удалено: {removed}");
                return 0;

            default:
                return 0;
        }
    }

    private static (bool Ok, int Removed) DeleteByProp(
        List<Shape> shapes, string? prop, ErrorLogger logger)
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
            logger.Error("[error] неверный формат --prop, ожидается key=value");
            return (false, 0);
        }

        var key = parts[0].Trim();
        var value = parts[1].Trim();

        if (!key.Equals("color", StringComparison.OrdinalIgnoreCase))
        {
            logger.Warn($"[warning] неизвестное свойство: {key}");
            return (true, 0);
        }

        if (!TryParseRgb(value, out var target))
        {
            logger.Error(
                "[error] значение color должно иметь вид RGB(r,g,b), r,g,b ∈ [0,255]");
            return (false, 0);
        }

        return (true, shapes.RemoveAll(s => target.Equals(s.Color)));
    }

    private static bool TryParseRgb(string value, out RGB rgb)
    {
        rgb = RGB.Black;

        var m = RgbRegex.Match(value);
        if (!m.Success) return false;

        if (!byte.TryParse(m.Groups[1].Value, out var r) ||
            !byte.TryParse(m.Groups[2].Value, out var g) ||
            !byte.TryParse(m.Groups[3].Value, out var b))
        {
            return false;
        }

        rgb = new RGB(r, g, b);
        return true;
    }
}