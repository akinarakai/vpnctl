public class FlagsHandler : IHandler
{
    private readonly IReadOnlyList<IInputFlag> _supportedFlags;

    public FlagsHandler(IReadOnlyList<IInputFlag> supportedFlags)
    {
        _supportedFlags = supportedFlags;
    }

    public bool CanHandle(InputContext input)
    {
        return input.Count == 0 &&
        (input.HasFlag<HelpFlag>() ||
        input.HasFlag<PurgeFlag>() ||
        input.HasFlag<LogsFlag>() ||
        input.HasFlag<StatusFlag>());
    }

    public void Handle(InputContext input)
    {
        foreach (var flag in input.Flags)
        {
            if (flag.Value is HelpFlag)
            {
                HandleHelp();
            }
            else if (flag.Value is StatusFlag)
            {
                HandleStatus(input.HasFlag<WatchFlag>());
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
        var response = ApiClient.Current.GetVpnLogs(logLines ?? 10);

        bool hasAnyActiveLogs = false;

        Console.WriteLine("===============================================================================================");

        foreach (var vpn in response.Vpns)
        {
            var name = FormatManager.GetVpnNameFromType(vpn.Type).ToUpper();

            Console.WriteLine($"SYSTEM LOGS FOR: {name}");

            var logs = vpn.LogsLines;

            if (logs.Count == 0)
            {
                Console.WriteLine("    ○ No recent log entries available is empty.");
            }
            else
            {
                hasAnyActiveLogs = true;

                foreach (var line in logs)
                {
                    Console.WriteLine($"    {line}");
                }
            }

            Console.WriteLine();
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

        ApiClient.Current.SendPurge();

        Logger.Success("System successfully cleared of all vpnctl traces!");
    }

    private void HandleStatus(bool isWatch)
    {
        ConsoleLiveView view;
        if (isWatch)
        {
            view = new ConsoleLiveView();
        }
        else
        {
            view = new ConsoleLiveView(1);
        }

        view.Start();

        while (view.KeepRunning())
        {
            var table = new ConsoleTable()
            {
                Width = 95
            };

            table.AddBorder();

            table.AddHeaders(
                new() { Name = "NAME", Spacing = 12 },
                new() { Name = "INSTALL STATUS", Spacing = 16 },
                new() { Name = "ENGINE STATE", Spacing = 14 },
                new() { Name = "PORT", Spacing = 15 },
                new() { Name = "CLIENTS", Spacing = 15 },
                new() { Name = "TRAFFIC (DN/UP)", Spacing = 10 }
            );

            table.AddSeparator();

            var api = ApiClient.Current;

            var vpns = api.GetVpns();

            int totalActiveClients = 0;
            int totalClients = 0;
            long globalBytesReceived = 0;
            long globalBytesSent = 0;

            foreach (var vpn in vpns.Vpns)
            {
                var name = FormatManager.GetVpnNameFromType(vpn.Type).ToUpper();

                string installText;
                string activeText;
                string portText = "-";
                string trafficFormat = "-";

                var clientsCountText = $"{vpn.Clients} / 0";

                totalClients += vpn.Clients;

                if (vpn.Installed == VpnInstallStatus.NOT_INSTALLED)
                {
                    installText = "○ NOT INSTALLED";
                    activeText = "-";
                }
                else
                {
                    installText = "● INSTALLED";

                    portText = string.Join("/", vpn.Ports);

                    if (vpn.Active == VpnActiveStatus.ACTIVE)
                    {
                        activeText = "● ACTIVE";

                        totalActiveClients += vpn.OnlineClients;
                        clientsCountText = $"{vpn.Clients} / {vpn.OnlineClients}";

                        var traffic = FormatManager.FormatTraffic(vpn.BytesReceived, vpn.BytesSent);
                        trafficFormat = $"{traffic.down} / {traffic.up}";

                        globalBytesReceived += vpn.BytesReceived;
                        globalBytesSent += vpn.BytesSent;
                    }
                    else
                    {
                        activeText = "○ INACTIVE";
                        trafficFormat = "0 B / 0 B";
                    }
                }

                table.AddRow(name, installText, activeText, portText, clientsCountText, trafficFormat);
            }

            table.AddBorder();

            var info = api.GetServerInfo();
            var monitor = api.GetSystemMonitor();

            var globalTraffic = FormatManager.FormatTraffic(globalBytesReceived, globalBytesSent);

            table.AddText($"Total Registered Clients: {totalClients} | Total Active Connections: {totalActiveClients}");
            table.AddText($"Total Server Traffic: Download: {globalTraffic.down} | Upload: {globalTraffic.up}");

            table.AddSeparator();

            table.AddText($"SERVER: IP: {info.Response.Ip} | Interface: {info.Response.NetworkInterface} | Hostname: {info.Response.Hostname} | Latency: {info.LatencyMs:0}ms");
            table.AddText($"SYSTEM: OS: {info.Response.Os} | Arch: {info.Response.Arch} | UTC: {info.Response.UtcTime:yyyy-MM-dd HH:mm:ss}");

            table.AddSeparator();

            table.AddText($"MONITOR: CPU: {monitor.CpuUsage}% | Load: {monitor.LoadAverage:0.00} | RAM: {monitor.UsageMemory} / {monitor.TotalMemory} MB | Uptime: {FormatManager.GetUptime(monitor.Uptime)}");

            table.AddBorder();

            foreach (var line in table.Build())
            {
                view.WriteLine(line);
            }

            view.Wait();
        }
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