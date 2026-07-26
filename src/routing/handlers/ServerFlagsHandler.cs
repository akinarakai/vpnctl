public class ServerFlagsHandler : IHandler
{
    private readonly IReadOnlyList<IInputFlag> _supportedFlags;

    public ServerFlagsHandler(IReadOnlyList<IInputFlag> supportedFlags)
    {
        _supportedFlags = supportedFlags;
    }

    public bool CanHandle(InputContext input)
    {
        if (input.HasArgs("wg", "vless", "xray", "awg")) return false;

        return input.Count > 0 &&
        (input.HasFlag<InitFlag>() ||
        input.HasFlag<HelpFlag>() ||
        input.HasFlag<PurgeFlag>() ||
        input.HasFlag<LogsFlag>() ||
        input.HasFlag<StatusFlag>());
    }

    public void Handle(InputContext input)
    {
        foreach (var flag in input.Flags)
        {
            if (flag.Value is InitFlag)
            {
                HandleInit();
            }
            else if (flag.Value is HelpFlag)
            {
                HandleHelp();
            }
            else if (flag.Value is StatusFlag)
            {
                HandleStatus();
            }
            else if (flag.Value is LogsFlag)
            {
                int? lines = null;
                if (input.TryGetFlag<LinesFlag>(out var linesFlag) && linesFlag?.Arguments?.Count > 0)
                {
                    if (int.TryParse(linesFlag.Arguments[0], out int parsedLines) && parsedLines > 0)
                    {
                        lines = parsedLines;
                    }
                }

                HandleLogs(lines);
            }
            else if (flag.Value is PurgeFlag)
            {
                var force = input.HasFlag<ForceFlag>();

                HandlePurge(force);
            }
        }
    }

    private void HandleLogs(int? logLines)
    {
        var vpns = VpnManager.GetAll();
        bool hasAnyActiveLogs = false;

        logLines = logLines != null ? logLines : 10;

        Console.WriteLine("===============================================================================================");

        foreach (var vpn in vpns)
        {
            var name = VpnHelper.GetNameFromType(vpn.Type).ToUpper();
            var installStatus = vpn.GetInstallStatus();
            var activeStatus = vpn.GetActiveStatus();

            if (installStatus == VpnInstallStatus.INSTALLED && activeStatus == VpnActiveStatus.ACTIVE)
            {
                hasAnyActiveLogs = true;

                Console.WriteLine($"SYSTEM LOGS FOR: {name}");

                var logContent = vpn.GetLogs(logLines.Value);

                if (string.IsNullOrEmpty(logContent))
                {
                    Console.WriteLine("    ○ No recent log entries available or journal is empty.");
                }
                else
                {
                    var lines = logContent.Split('\n');
                    foreach (var line in lines)
                    {
                        Console.WriteLine($"    {line}");
                    }
                }

                Console.WriteLine();
            }
        }

        if (!hasAnyActiveLogs)
        {
            Console.WriteLine("  No active VPN engines found. Run a service before checking system logs.");
        }

        Console.WriteLine("===============================================================================================");
    }

    private void HandlePurge(bool force)
    {
        if (!force)
        {
            Logger.Warn("WARNING! This command will completely remove all VPN services, reset firewall rules, and revert Linux system settings to defaults.");
            Logger.Warn("This action is permanent and CANNOT be undone.");

            var random = new Random();
            int confirmationCode = random.Next(1000, 10000);

            Console.Write($"To confirm deletion, please type the verification code [");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(confirmationCode);
            Console.ResetColor();
            Console.Write("]: ");

            string? response = Console.ReadLine()?.Trim();

            if (response != confirmationCode.ToString())
            {
                Logger.Info("Purge operation cancelled by user (verification failed).");
                return;
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Logger.Info("Code verified. Starting purge process...");
            Console.ResetColor();
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

        var data = Kernel.Data;
        var server = data.GetServerState();
        var clients = data.GetClientsState().Clients;

        int totalActiveClients = 0;
        long globalBytesReceived = 0;
        long globalBytesSent = 0;

        Console.WriteLine("===============================================================================================");
        Console.WriteLine($"  {"NAME",-12} {"INSTALL STATUS",-16} {"ENGINE STATE",-14} {"PORT",-15} {"CLIENTS",-12} {"TRAFFIC (DN/UP)"}");
        Console.WriteLine("  ---------------------------------------------------------------------------------------------");

        foreach (var vpn in vpns)
        {
            var name = VpnHelper.GetNameFromType(vpn.Type).ToUpper();
            var installStatus = vpn.GetInstallStatus();
            var activeStatus = vpn.GetActiveStatus();

            string installText;
            string activeText;
            string clientsCountText = "-";
            string portText = "-";
            string trafficFormat = "-";

            if (installStatus == VpnInstallStatus.NOT_INSTALLED)
            {
                installText = "○ NOT INSTALLED";
                activeText = "-";
            }
            else
            {
                installText = "● INSTALLED";

                if (vpn.Type == VpnServiceType.WIREGUARD)
                    portText = server.Wg.Port.ToString();
                else if (vpn.Type == VpnServiceType.AMNEZIAWG)
                    portText = server.Awg.Port.ToString();
                else if (vpn.Type == VpnServiceType.XRAY)
                {
                    var vlessPort = server.Xray.Vless.Port;
                    var socksPort = server.Xray.Socks.Port;
                    var ssPort = server.Xray.Shadowsocks.Port;

                    portText = $"{vlessPort}/{socksPort}/{ssPort}";
                }

                var onlineStats = vpn.GetOnlineStats() ?? new List<ClientOnlineStats>();
                int totalClientsForVpn = onlineStats.Count;

                if (activeStatus == VpnActiveStatus.ACTIVE)
                {
                    activeText = "● ACTIVE";

                    int onlineClientsForVpn = onlineStats.Count(c => c.LastConnectAt.HasValue &&
                        (DateTime.UtcNow - c.LastConnectAt.Value).TotalMinutes < 3);

                    totalActiveClients += onlineClientsForVpn;
                    clientsCountText = $"{totalClientsForVpn} / [{onlineClientsForVpn}]";

                    long vpnBytesReceived = onlineStats.Sum(o => o.BytesRecived);
                    long vpnBytesSent = onlineStats.Sum(o => o.BytesSent);

                    var traffic = FormatManager.FormatTraffic(vpnBytesReceived, vpnBytesSent);
                    trafficFormat = $"{traffic.down} / {traffic.up}";

                    globalBytesReceived += vpnBytesReceived;
                    globalBytesSent += vpnBytesSent;
                }
                else
                {
                    activeText = "○ INACTIVE";
                    clientsCountText = $"{totalClientsForVpn} / 0";
                    trafficFormat = "0 B / 0 B";
                }
            }

            Console.WriteLine($"  {name,-12} {installText,-16} {activeText,-14} {portText,-15} {clientsCountText,-12} {trafficFormat}");
        }

        Console.WriteLine("===============================================================================================");

        var globalTraffic = FormatManager.FormatTraffic(globalBytesReceived, globalBytesSent);

        Console.WriteLine($"  Total Registered Clients: {clients.Count} | Total Active Connections: {totalActiveClients}");
        Console.WriteLine($"  Total Server Traffic: Download: {globalTraffic.down} | Upload: {globalTraffic.up}");
        Console.WriteLine($"  Server IP: {Kernel.Network.GetIP()} | Network Interface: {server.NetworkInterface}");
        Console.WriteLine($"  WireGuard Subnet: {server.Wg!.Subnet} | AmneziaWG Subnet: {server.Awg!.Subnet}");
    }

    private void HandleHelp()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine(" vpnctl [arguments] [flags]");

        Console.WriteLine("\nCommands:");
        PrintCommand("<vpn_name> --install", "Install the specified provider");
        PrintCommand("<vpn_name> --uninstall", "Remove the specified provider");
        PrintCommand("<vpn_name> --restart", "Remove the specified provider");
        PrintCommand("<vpn_name> --up", "Enable and start the provider connection");
        PrintCommand("<vpn_name> --down", "Disable and stop the provider connection");
        PrintCommand("<vpn_name> --show", "Get the current running status of the provider");
        PrintCommand("<vpn_name> client add --name <name> --password <pwd>", "Create a new configuration file");
        PrintCommand("client del <name>", "Delete an existing configuration file");
        PrintCommand("client up/down <name>", "Set active client");
        PrintCommand("client list --qr/--cfg", "List all configurations.");
        PrintCommand("client <name> --qr/--cfg", "Get a specific configuration.");
        PrintCommand("--purge", "Delete vpnctl");

        if (_supportedFlags.Count > 0)
        {
            Console.WriteLine("\nAvailable Flags:");

            foreach (var flag in _supportedFlags)
            {
                string longName = $"--{flag.Name}";
                string shortName = flag.ShortName != null ? $"-{flag.ShortName}" : "";

                string flagSyntax = string.IsNullOrEmpty(shortName) ? $" {longName}" : $" {longName}, {shortName}";

                Console.WriteLine($"{flagSyntax,-32} {flag.Description}");
            }
        }
    }

    private void PrintCommand(string command, string description)
    {
        Console.WriteLine($" {command,-32} {description}");
    }
}