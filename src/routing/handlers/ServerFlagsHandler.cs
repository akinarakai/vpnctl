public class ServerFlagsHandler : IHandler
{
    private readonly IReadOnlyList<IInputFlag> _supportedFlags;

    public ServerFlagsHandler(IReadOnlyList<IInputFlag> supportedFlags)
    {
        _supportedFlags = supportedFlags;
    }

    public bool CanHandle(InputContext input)
    {
        return input.Count > 0 &&
        (input.HasFlag<InitFlag>() ||
        input.HasFlag<HelpFlag>() ||
        input.HasFlag<PurgeFlag>() ||
        input.HasFlag<StatusFlag>());
    }

    public void Handle(InputContext input)
    {
        foreach (var flag in input.Flags)
        {
            if (flag.Value is InitFlag)
            {
                HandleInit();
                Console.WriteLine();
            }
            else if (flag.Value is HelpFlag)
            {
                HandleHelp();
                Console.WriteLine();
            }
            else if (flag.Value is StatusFlag)
            {
                HandleStatus();
                Console.WriteLine();
            }
            else if (flag.Value is PurgeFlag)
            {
                var force = input.HasFlag<ForceFlag>();
                
                HandlePurge(force);
                Console.WriteLine();
            }
        }
    }

    private void HandlePurge(bool force)
    {
        if (!force)
        {
            Logger.Warn("WARNING! This command will completely remove all VPN services, reset firewall rules, and revert Linux system settings to defaults.");
            Console.Write("Are you sure you want to continue? [y/N]: ");

            string? response = Console.ReadLine()?.Trim().ToLower();
            if (response != "y" && response != "yes")
            {
                Logger.Info("Purge operation cancelled by user.");
                return;
            }
        }

        Logger.Info("Stopping and removing VPN services...");

        var vpns = VpnManager.GetAll();
        foreach (var vpn in vpns)
        {
            var name = VpnHelper.GetNameFromType(vpn.Type).ToUpper();
            if (vpn.GetInstallStatus() == VpnInstallStatus.INSTALLED)
            {
                try
                {
                    Logger.Info($"Stopping and purging {name}...");
                    vpn.Uninstall();
                }
                catch (Exception ex)
                {
                    Logger.Error($"Failed to uninstall {name}: {ex.Message}");
                }
            }
        }

        // Firewall
        try
        {
            //Logger.Info("Clearing firewall rules...");
            // Kernel.Firewall.RemoveAllVpnRules();
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to reset firewall: {ex.Message}");
        }

        // Sys config
        try
        {
            Logger.Info("Reverting sysctl system optimizations...");
            Kernel.SysConfig.DeleteSystemConfig();
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to delete system configuration: {ex.Message}");
        }

        // Storage
        try
        {
            Logger.Info("Deleting application configuration files...");
            Kernel.Data.DeleteFiles();
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to clear data files: {ex.Message}");
        }

        Logger.Success("System successfully cleared of all vpnctl traces!");
    }

    private void HandleInit()
    {
        Logger.Info("Initialization data...");

        var data = Kernel.Data;

        var server = data.GetServerState();
        var clients = data.GetClientsState();

        Kernel.SysConfig.ApplySystemOptimizations();

        data.GetServerState().NetworkInterface = Kernel.Network.GetActiveInterface();

        Logger.Success($"System initialized successfully.");
    }

    private void HandleStatus()
    {
        var vpns = VpnManager.GetAll();

        Console.WriteLine("=================================================");

        Console.WriteLine($"  {"NAME",-15} {"INSTALL STATUS",-18} {"ENGINE STATE",-15}");
        Console.WriteLine("  -----------------------------------------------");

        foreach (var vpn in vpns)
        {
            var name = VpnHelper.GetNameFromType(vpn.Type).ToUpper();
            var installStatus = vpn.GetInstallStatus();
            var activeStatus = vpn.GetActiveStatus();

            string installText;
            string activeText;

            if (installStatus == VpnInstallStatus.NOT_INSTALLED)
            {
                installText = "○ NOT INSTALLED";
                activeText = "absent";
            }
            else
            {
                installText = "● INSTALLED";

                if (activeStatus == VpnActiveStatus.ACTIVE)
                {
                    activeText = "● ACTIVE";
                }
                else
                {
                    activeText = "○ INACTIVE";
                }
            }

            Console.WriteLine($"  {name,-15} {installText,-18} {activeText,-15}");
        }

        Console.WriteLine("=================================================");
    }

    private void HandleHelp()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("vpnctl [arguments]");

        Console.WriteLine("\nProvider Commands:");
        PrintCommand("<provider_name> install", "Install the specified provider");
        PrintCommand("<provider_name> uninstall", "Remove the specified provider");
        PrintCommand("<provider_name> restart", "Remove the specified provider");
        PrintCommand("<provider_name> up", "Enable and start the provider connection");
        PrintCommand("<provider_name> down", "Disable and stop the provider connection");
        PrintCommand("<provider_name> show", "Get the current running status of the provider");
        PrintCommand("<provider_name> cfg create <name>", "Create a new configuration file");
        PrintCommand("<provider_name> cfg remove <name>", "Delete an existing configuration file");
        PrintCommand("<provider_name> help", "Display help information for the specific provider");

        Console.WriteLine("\nGlobal Commands:");
        PrintCommand("help", "Display general help information for vpnctl");
        PrintCommand("cfg list [qr]", "List all configurations. Append 'qr' to generate a QR code");
        PrintCommand("cfg <name> [qr]", "Get a specific configuration. Append 'qr' to generate a QR code");

        if (_supportedFlags.Count > 0)
        {
            Console.WriteLine("\nAvailable Flags:");

            foreach (var flag in _supportedFlags)
            {
                string longName = $"--{flag.Name}";
                string shortName = flag.ShortName != null ? $"-{flag.ShortName}" : "";

                string flagSyntax = string.IsNullOrEmpty(shortName) ? $"  {longName}" : $"  {longName}, {shortName}";

                Console.WriteLine($"{flagSyntax,-20} {flag.Description}");
            }
        }

        Console.WriteLine("\nExamples:");
        Console.WriteLine("vpnctl wg up");
        Console.WriteLine("vpnctl wg cfg create my_profile");
        Console.WriteLine("vpnctl cfg list qr");
    }

    private void PrintCommand(string command, string description)
    {
        Console.WriteLine($"{command} - {description}");
    }
}