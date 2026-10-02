namespace FolderSync;

public sealed class FileLogger : ISyncLogger, IDisposable
{
    private readonly StreamWriter _writer;
    private readonly object _lock = new();

    public FileLogger(string logFilePath)
    {
        var directory = Path.GetDirectoryName(logFilePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        _writer = new StreamWriter(logFilePath, append: true) { AutoFlush = true };
    }

    public void Info(string message) => Write("INFO", message);

    public void Error(string message) => Write("ERROR", message);

    private void Write(string level, string message)
    {
        var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}";

        lock (_lock)
        {
            Console.WriteLine(line);
            _writer.WriteLine(line);
        }
    }

    public void Dispose() => _writer.Dispose();
    
}