using FolderSync;

namespace FolderSync.Tests;

public sealed class FolderSynchronizerTests : IDisposable
{
    private readonly string _rootDir;
    private readonly string _sourceDir;
    private readonly string _replicaDir;
    private readonly TestLogger _logger = new();
    private readonly FolderSynchronizer _sut;

    public FolderSynchronizerTests()
    {
        _rootDir = Path.Combine(Path.GetTempPath(), "FolderSyncTests_" + Guid.NewGuid());
        _sourceDir = Path.Combine(_rootDir, "source");
        _replicaDir = Path.Combine(_rootDir, "replica");
        Directory.CreateDirectory(_sourceDir);
        Directory.CreateDirectory(_replicaDir);
        _sut = new FolderSynchronizer(_logger);
    }

    [Fact]
    public void Synchronize_CopiesNewFileFromSourceToReplica()
    {
        File.WriteAllText(Path.Combine(_sourceDir, "a.txt"), "hello");

        _sut.Synchronize(_sourceDir, _replicaDir);

        Assert.True(File.Exists(Path.Combine(_replicaDir, "a.txt")));
    }

    [Fact]
    public void Synchronize_OverwritesModifiedFile()
    {
        var sourceFile = Path.Combine(_sourceDir, "a.txt");
        File.WriteAllText(sourceFile, "version 1");
        _sut.Synchronize(_sourceDir, _replicaDir);

        File.WriteAllText(sourceFile, "version 2");
        _sut.Synchronize(_sourceDir, _replicaDir);

        Assert.Equal("version 2", File.ReadAllText(Path.Combine(_replicaDir, "a.txt")));
    }

    [Fact]
    public void Synchronize_RemovesFileDeletedFromSource()
    {
        var sourceFile = Path.Combine(_sourceDir, "a.txt");
        File.WriteAllText(sourceFile, "hello");
        _sut.Synchronize(_sourceDir, _replicaDir);

        File.Delete(sourceFile);
        _sut.Synchronize(_sourceDir, _replicaDir);

        Assert.False(File.Exists(Path.Combine(_replicaDir, "a.txt")));
    }

    [Fact]
    public void Synchronize_MirrorsNestedDirectoriesIncludingEmptyOnes()
    {
        Directory.CreateDirectory(Path.Combine(_sourceDir, "sub", "empty"));
        File.WriteAllText(Path.Combine(_sourceDir, "sub", "b.txt"), "nested");

        _sut.Synchronize(_sourceDir, _replicaDir);

        Assert.True(Directory.Exists(Path.Combine(_replicaDir, "sub", "empty")));
        Assert.True(File.Exists(Path.Combine(_replicaDir, "sub", "b.txt")));
    }

    [Fact]
    public void Synchronize_RemovesDirectoryDeletedFromSource()
    {
        var sourceSub = Path.Combine(_sourceDir, "sub");
        Directory.CreateDirectory(sourceSub);
        File.WriteAllText(Path.Combine(sourceSub, "b.txt"), "nested");
        _sut.Synchronize(_sourceDir, _replicaDir);

        Directory.Delete(sourceSub, recursive: true);
        _sut.Synchronize(_sourceDir, _replicaDir);

        Assert.False(Directory.Exists(Path.Combine(_replicaDir, "sub")));
    }

    [Fact]
    public void Synchronize_DoesNotRewriteUnchangedFile()
    {
        var sourceFile = Path.Combine(_sourceDir, "a.txt");
        File.WriteAllText(sourceFile, "hello");
        _sut.Synchronize(_sourceDir, _replicaDir);

        var replicaFile = Path.Combine(_replicaDir, "a.txt");
        var writeTimeBefore = File.GetLastWriteTimeUtc(replicaFile);

        Thread.Sleep(50);
        _sut.Synchronize(_sourceDir, _replicaDir);

        Assert.Equal(writeTimeBefore, File.GetLastWriteTimeUtc(replicaFile));
    }

    [Fact]
    public void Synchronize_DetectsSameSizeDifferentContent()
    {
        var sourceFile = Path.Combine(_sourceDir, "a.txt");
        File.WriteAllText(sourceFile, "AAAA");
        _sut.Synchronize(_sourceDir, _replicaDir);

        File.WriteAllText(sourceFile, "BBBB");
        _sut.Synchronize(_sourceDir, _replicaDir);

        Assert.Equal("BBBB", File.ReadAllText(Path.Combine(_replicaDir, "a.txt")));
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootDir))
        {
            Directory.Delete(_rootDir, recursive: true);
        }
    }
}