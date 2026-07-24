using System.Diagnostics;

public class Program
{
    public static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            Logger.Warn("Arguments were not provided!");
            return;
        }

        var timer = Stopwatch.StartNew();

        Kernel.Register<ICommandRunner>(() => new LinuxCommandRunner());
        Kernel.Register<IDataProvider>(() => new JsonDataProvider());
        Kernel.Register<IFirewallManager>(() => new UfwFirewallManager());
        Kernel.Register<INetworkManager>(() => new NetworkManager());
        Kernel.Register<ISystemConfigurator>(() => new SystemConfigurator());

        VpnManager.Register(() => new WireGuard());
        VpnManager.Register(() => new AmneziaWg());
        VpnManager.Register(() => new Xray());

        var flagsList = FlagsInstance.GetAll();

        var router = new ArgsRouter();
        router.AddHandler(() => new ServerFlagsHandler(flagsList));
        router.AddHandler(() => new ClientActionHandler());
        router.AddHandler(() => new ClientsShowHandler());
        router.AddHandler(() => new VpnFlagsHandler());

        // EXTRA
        router.AddHandler(() => new WgFlagsHandler());
        router.AddHandler(() => new AwgFlagsHandler());
        router.AddHandler(() => new XrayFlagsHandler());

        try
        {
            var input = new InputContext(args, flagsList);

            if (!router.Route(input))
            {
                Logger.Warn("Failed to execute the command.");
                return;
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Internal error: {ex.Message}");
        }
        finally
        {
            try
            {
                if (Kernel.IsCreated<IDataProvider>())
                {
                    Kernel.Data.TrySave();
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to write transactional state: {ex.Message}");
            }

            timer.Stop();
            Logger.Info($"Execution completed in {timer.Elapsed.TotalSeconds:F2} seconds.");
        }
    }
}