namespace FolderSync;

public sealed record SyncOptions(
    string SourcePath,
    string ReplicaPath,
    TimeSpan Interval,
    string LogFilePath)
{
    public static SyncOptions Parse(string[] args)
    {
        if (args.Length != 4)
        {
            throw new ArgumentException(
                "Usage: FolderSync <sourcePath> <replicaPath> <intervalSeconds> <logFilePath>");
        }

        var sourcePath = Path.GetFullPath(args[0]);
        var replicaPath = Path.GetFullPath(args[1]);

        if (!Directory.Exists(sourcePath))
        {
            throw new ArgumentException($"Source folder does not exist: {sourcePath}");
        }

        if (!double.TryParse(args[2], out var intervalSeconds) || intervalSeconds <= 0)
        {
            throw new ArgumentException($"Sync interval must be a positive number of seconds: '{args[2]}'");
        }

        if (string.Equals(sourcePath, replicaPath, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Source and replica paths must be different.");
        }

        if (IsSubPath(sourcePath, replicaPath) || IsSubPath(replicaPath, sourcePath))
        {
            throw new ArgumentException("Source and replica folders cannot be nested inside each other.");
        }

        var logFilePath = Path.GetFullPath(args[3]);

        return new SyncOptions(sourcePath, replicaPath, TimeSpan.FromSeconds(intervalSeconds), logFilePath);
    }

    private static bool IsSubPath(string basePath, string candidate)
    {
        var baseWithSeparator = basePath.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        return candidate.StartsWith(baseWithSeparator, StringComparison.OrdinalIgnoreCase);
    }
}