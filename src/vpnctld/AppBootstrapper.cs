public class AppBootstrapper
{
    public WebApplication Build(string[] args)
    {
        if (!OperatingSystem.IsLinux())
        {
            throw new PlatformNotSupportedException("vpnctld is only supported on Linux.");
        }

        var builder = WebApplication.CreateBuilder(args);

        ConfigureServices(builder);
        ConfigureKernel();

        return builder.Build();
    }

    private void ConfigureServices(WebApplicationBuilder builder)
    {
        builder.Services.AddHostedService<StorageBackgroundService>();
        builder.Logging.ClearProviders();
    }

    private void ConfigureKernel()
    {
        Kernel.Register<ICommandRunner>(() => new LinuxCommandRunner());
        Kernel.Register<IFileManager>(() => new BaseFileManager());
        Kernel.Register<IDataProvider>(() => new JsonDataProvider());
        Kernel.Register<IFirewallManager>(() => new UfwFirewallManager());
        Kernel.Register<INetworkManager>(() => new NetworkManager());
        Kernel.Register<ISystemConfigurator>(() => new SystemConfigurator());
        Kernel.Register<ISystemMonitor>(() => new BaseSystemMonitor());

        VpnManager.Register(() => new WireGuard());
        VpnManager.Register(() => new AmneziaWg());
        VpnManager.Register(() => new Xray());
    }
}