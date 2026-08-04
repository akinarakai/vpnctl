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

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (Kernel.IsCreated<IDataProvider>())
        {
            Kernel.Get<IDataProvider>().TrySave();
        }

        await base.StopAsync(cancellationToken);
    }
}