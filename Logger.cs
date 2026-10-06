using System.ComponentModel;
using System.Globalization;

namespace ShapesCli;

public static class ErrorLogger
{
    private static readonly string DefaultLogFile = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ShapesCli",
        "errors.log");

    private static readonly List<string> Messages = new();

    public static void Warn(string message) => Add(message);
    public static void Error(string message) => Add(message);

    private static void Add(string message)
    {
        Console.Error.WriteLine(message);
        Messages.Add(message);
        AppendLine(DefaultLogFile, message);
    }

    public static void Save(string? path)
    {
        if (path is not null)
            SaveToFile(path);
    }

    private static void SaveToFile(string path)
    {
        try
        {
            var dir = Path.GetDirectoryName(Path.GetFullPath(path));
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllLines(path, Messages);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(
                $"[error] не удалось записать лог в {path}: {Describe(ex)}");
        }
    }

    private static void AppendLine(string path, string message)
    {
        try
        {
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            File.AppendAllText(path, message + Environment.NewLine);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(
                $"[error] не удалось записать лог в {path}: {Describe(ex)}");
        }
    }

    private static string Describe(Exception ex)
    {
        var win32 = new Win32Exception(ex.HResult);

        if (win32.NativeErrorCode == 0 || win32.Message == ex.Message)
            return ex.Message;

        return $"{ex.Message} (errno {win32.NativeErrorCode}: {win32.Message})";
    }
}

public static class LastLogState
{
    private static string StateFile => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ShapesCli",
        "last_log.txt");

    public static string? Read()
    {
        try
        {
            if (!File.Exists(StateFile)) return null;
            var text = File.ReadAllText(StateFile).Trim();
            return text.Length == 0 ? null : text;
        }
        catch
        {
            return null;
        }
    }

    public static void Write(string path)
    {
        try
        {
            var dir = Path.GetDirectoryName(StateFile);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(StateFile, path);
        }
        catch
        {
        }
    }
}