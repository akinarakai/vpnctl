public class StorageBackgroundService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (Kernel.IsCreated<IDataProvider>())
            {
                Kernel.Get<IDataProvider>().TrySave();
            }

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}