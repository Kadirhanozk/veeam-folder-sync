using FolderSync;

namespace FolderSync.Tests;

internal sealed class TestLogger : ISyncLogger
{
    public List<string> Messages { get; } = new();

    public void Info(string message) => Messages.Add($"INFO: {message}");

    public void Error(string message) => Messages.Add($"ERROR: {message}");
}