using System.Security.Cryptography;

namespace FolderSync;

public sealed class FolderSynchronizer
{
    private readonly ISyncLogger _logger;

    public FolderSynchronizer(ISyncLogger logger)
    {
        _logger = logger;
    }

    public void Synchronize(string sourceDir, string replicaDir)
    {
        Directory.CreateDirectory(replicaDir);

        MirrorDirectory(sourceDir, replicaDir);
        RemoveExtras(sourceDir, replicaDir);
    }

    private void MirrorDirectory(string sourceDir, string replicaDir)
    {
        foreach (var sourceSubDir in SafeEnumerateDirectories(sourceDir))
        {
            var replicaSubDir = Path.Combine(replicaDir, Path.GetFileName(sourceSubDir));

            if (!Directory.Exists(replicaSubDir))
            {
                Directory.CreateDirectory(replicaSubDir);
                _logger.Info($"Directory created: {replicaSubDir}");
            }

            MirrorDirectory(sourceSubDir, replicaSubDir);
        }

        foreach (var sourceFile in SafeEnumerateFiles(sourceDir))
        {
            var replicaFile = Path.Combine(replicaDir, Path.GetFileName(sourceFile));

            try
            {
                if (!File.Exists(replicaFile))
                {
                    File.Copy(sourceFile, replicaFile, overwrite: true);
                    _logger.Info($"File created: {replicaFile}");
                }
                else if (!FilesAreEqual(sourceFile, replicaFile))
                {
                    File.Copy(sourceFile, replicaFile, overwrite: true);
                    _logger.Info($"File updated: {replicaFile}");
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                _logger.Error($"Failed to copy '{sourceFile}' to '{replicaFile}': {ex.Message}");
            }
        }
    }

    private void RemoveExtras(string sourceDir, string replicaDir)
    {
        foreach (var replicaFile in SafeEnumerateFiles(replicaDir))
        {
            var sourceFile = Path.Combine(sourceDir, Path.GetFileName(replicaFile));
            if (File.Exists(sourceFile))
            {
                continue;
            }

            try
            {
                File.Delete(replicaFile);
                _logger.Info($"File deleted: {replicaFile}");
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                _logger.Error($"Failed to delete '{replicaFile}': {ex.Message}");
            }
        }

        foreach (var replicaSubDir in SafeEnumerateDirectories(replicaDir))
        {
            var sourceSubDir = Path.Combine(sourceDir, Path.GetFileName(replicaSubDir));

            if (!Directory.Exists(sourceSubDir))
            {
                try
                {
                    Directory.Delete(replicaSubDir, recursive: true);
                    _logger.Info($"Directory deleted: {replicaSubDir}");
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    _logger.Error($"Failed to delete directory '{replicaSubDir}': {ex.Message}");
                }

                continue;
            }

            RemoveExtras(sourceSubDir, replicaSubDir);
        }
    }

    private static bool FilesAreEqual(string first, string second)
    {
        var firstInfo = new FileInfo(first);
        var secondInfo = new FileInfo(second);

        if (firstInfo.Length != secondInfo.Length)
        {
            return false;
        }

        using var firstStream = File.OpenRead(first);
        using var secondStream = File.OpenRead(second);
        using var sha256 = SHA256.Create();

        var firstHash = sha256.ComputeHash(firstStream);
        var secondHash = sha256.ComputeHash(secondStream);

        return firstHash.AsSpan().SequenceEqual(secondHash);
    }

    private IEnumerable<string> SafeEnumerateFiles(string path)
    {
        try
        {
            return Directory.EnumerateFiles(path).ToList();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.Error($"Failed to list files in '{path}': {ex.Message}");
            return Enumerable.Empty<string>();
        }
    }

    private IEnumerable<string> SafeEnumerateDirectories(string path)
    {
        try
        {
            return Directory.EnumerateDirectories(path).ToList();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.Error($"Failed to list directories in '{path}': {ex.Message}");
            return Enumerable.Empty<string>();
        }
    }
}