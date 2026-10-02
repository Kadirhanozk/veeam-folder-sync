namespace FolderSync;

public interface ISyncLogger
{
    void Info(string message);

    void Error(string message);
}