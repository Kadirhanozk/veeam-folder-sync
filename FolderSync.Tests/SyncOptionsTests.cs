using FolderSync;

namespace FolderSync.Tests;

public sealed class SyncOptionsTests
{
    [Fact]
    public void Parse_ThrowsWhenSourceAndReplicaAreTheSamePath()
    {
        var tempDir = Directory.CreateTempSubdirectory().FullName;
        try
        {
            Assert.Throws<ArgumentException>(() =>
                SyncOptions.Parse(new[] { tempDir, tempDir, "5", "log.txt" }));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void Parse_ThrowsWhenSourceFolderDoesNotExist()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), "FolderSyncTests_missing_" + Guid.NewGuid());

        Assert.Throws<ArgumentException>(() =>
            SyncOptions.Parse(new[] { missingPath, Path.GetTempPath(), "5", "log.txt" }));
    }

    [Fact]
    public void Parse_ThrowsWhenIntervalIsNotAPositiveNumber()
    {
        var tempDir = Directory.CreateTempSubdirectory().FullName;
        try
        {
            Assert.Throws<ArgumentException>(() =>
                SyncOptions.Parse(new[] { tempDir, Path.Combine(tempDir, "..", "replica"), "0", "log.txt" }));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void Parse_ThrowsWhenReplicaIsNestedInsideSource()
    {
        var sourceDir = Directory.CreateTempSubdirectory().FullName;
        var replicaDir = Path.Combine(sourceDir, "replica");
        try
        {
            Assert.Throws<ArgumentException>(() =>
                SyncOptions.Parse(new[] { sourceDir, replicaDir, "5", "log.txt" }));
        }
        finally
        {
            Directory.Delete(sourceDir, recursive: true);
        }
    }
}