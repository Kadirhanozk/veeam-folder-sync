namespace FolderSync;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        SyncOptions options;
        try
        {
            options = SyncOptions.Parse(args);
        }
        catch (ArgumentException ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }

        using var logger = new FileLogger(options.LogFilePath);
        var synchronizer = new FolderSynchronizer(logger);

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            logger.Info("Shutdown requested, stopping after the current sync cycle...");
            cts.Cancel();
        };

        logger.Info($"Starting sync: '{options.SourcePath}' -> '{options.ReplicaPath}' every {options.Interval}.");

        RunSyncCycle(synchronizer, logger, options);

        using var timer = new PeriodicTimer(options.Interval);
        try
        {
            while (await timer.WaitForNextTickAsync(cts.Token))
            {
                RunSyncCycle(synchronizer, logger, options);
            }
        }
        catch (OperationCanceledException)
        {
            //When Ctrl+C triggers cancellation
        }

        logger.Info("Sync stopped.");
        return 0;
    }

    private static void RunSyncCycle(FolderSynchronizer synchronizer, ISyncLogger logger, SyncOptions options)
    {
        try
        {
            synchronizer.Synchronize(options.SourcePath, options.ReplicaPath);
        }
        catch (Exception ex)
        {
            logger.Error($"Sync cycle failed: {ex.Message}");
        }
    }
}