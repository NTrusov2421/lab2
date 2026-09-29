using System.Globalization;

namespace ShapesCli;

// Собирает все сообщения и в конце работы сохраняет их:
// - в файл, если Save(path) получил путь;
// - в переменные окружения SHAPESCLI_ERRORS / SHAPESCLI_ERROR_COUNT, если path == null.
public sealed class ErrorLogger
{
    private readonly List<string> _messages = new();

    public void Warn(string message) => Add(message);
    public void Error(string message) => Add(message);

    private void Add(string message)
    {
        Console.Error.WriteLine(message);
        _messages.Add(message);
    }

    public void Save(string? path)
    {
        if (path is null)
            SaveToEnv();
        else
            SaveToFile(path);
    }

    private void SaveToFile(string path)
    {
        try
        {
            var dir = Path.GetDirectoryName(Path.GetFullPath(path));
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllLines(path, _messages);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(
                $"[error] не удалось записать лог в {path}: {ex.Message}");
        }
    }

    private void SaveToEnv()
    {
        Environment.SetEnvironmentVariable(
            "SHAPESCLI_ERRORS",
            string.Join(Environment.NewLine, _messages));
        Environment.SetEnvironmentVariable(
            "SHAPESCLI_ERROR_COUNT",
            _messages.Count.ToString(CultureInfo.InvariantCulture));
    }
}

// Хранит путь к последнему использованному файлу лога.
// Один текстовый файл в каталоге ApplicationData.
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