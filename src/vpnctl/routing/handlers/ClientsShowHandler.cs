public enum ConfigDisplayMode
{
    None,
    Cfg,
    Qr
}

public class ClientsShowHandler : IHandler
{
    public bool CanHandle(InputContext input)
    {
        // client name / client list
        if (input.Count != 2) return false;

        var secondWord = input.Args[1].ToLower();
        if (secondWord == "del" || secondWord == "up" || secondWord == "down" || secondWord == "add")
            return false;

        return true;
    }

    public void Handle(InputContext input)
    {
        var data = Kernel.Data;

        var clients = data.GetClientsState().Clients;
        if (clients.Count == 0)
        {
            Logger.Warn("No clients found.");
            return;
        }

        var isSingle = input.Args[1] != "list";

        var displayMode = ConfigDisplayMode.None;
        if (input.HasFlag<QrFlag>())
        {
            displayMode = ConfigDisplayMode.Qr;
        }
        else if (input.HasFlag<CfgFlag>())
        {
            displayMode = ConfigDisplayMode.Cfg;
        }

        if (isSingle)
        {
            var name = input.Args[1];

            var client = data.GetClient(name);
            if (client == null)
            {
                Logger.Warn($"Cannot found client: {name}.");
                return;
            }

            var stats = GetStatsForClient(client);
            PrintConfig(client, stats, displayMode);
        }
        else
        {
            for (int i = 0; i < clients.Count; i++)
            {
                var client = clients[i];

                var stats = GetStatsForClient(client);
                PrintConfig(client, stats, displayMode, i + 1);
            }
        }
    }

    private void PrintConfig(VpnClientBase client, ClientOnlineStats? stats, ConfigDisplayMode displayMode, int? iter = null)
    {
        var providerStr = client switch
        {
            WireGuardClient => "WireGuard",
            AmneziaWgClient => "AmneziaWG",
            VlessClient => "VLESS",
            SocksClient => "SOCKS",
            ShadowsocksClient => "Shadowsocks",
            _ => "Unknown"
        };

        var activeStr = client.IsActive ? "● ENABLED" : "○ BLOCKED";

        var dateStr = client.CreatedAt.ToLocalTime().ToString("dd.MM.yyyy");
        var interStr = iter != null ? $"{iter}. " : "";

        var networkStatusStr = "○ OFFLINE";
        if (stats != null && stats.LastConnectAt.HasValue)
        {
            var timeSinceLastConnect = DateTime.UtcNow - stats.LastConnectAt.Value;
            if (timeSinceLastConnect.TotalMinutes <= 3)
            {
                networkStatusStr = "● ONLINE";
            }
        }

        Console.WriteLine($"{interStr}{client.Name} ({client.Id}) | {providerStr} | {activeStr} | {networkStatusStr} | Created: {dateStr}");

        if (stats != null)
        {
            var lastConnectStr = stats.LastConnectAt.HasValue
                ? FormatManager.GetRelativeTime(stats.LastConnectAt.Value)
                : "Never";

            var endpointStr = string.IsNullOrEmpty(stats.Endpoint) ? "None" : stats.Endpoint;

            var (downStr, upStr) = FormatManager.FormatTraffic(stats.BytesRecived, stats.BytesSent);

            Console.WriteLine($"  -> Endpoint: {endpointStr}");
            Console.WriteLine($"  -> Last Activity: {lastConnectStr}");
            Console.WriteLine($"  -> Traffic: Download: {downStr} | Upload: {upStr}");
        }
        else if (client is WireGuardClient || client is AmneziaWgClient)
        {
            Console.WriteLine("  -> Statistics: No data available (Offline)");
        }

        if (displayMode != ConfigDisplayMode.None && !string.IsNullOrEmpty(client.ConfigStr))
        {
            if (displayMode == ConfigDisplayMode.Qr)
            {
                Console.WriteLine("  -- QR CODE --");
                QRCode.Render(client.ConfigStr);
            }
            else if (displayMode == ConfigDisplayMode.Cfg)
            {
                Console.WriteLine("  -- CONFIGURATION --");
                Console.WriteLine(client.ConfigStr);
            }
        }

        Console.WriteLine("--------------------------------------------------");
        Console.WriteLine();
    }

    private ClientOnlineStats? GetStatsForClient(VpnClientBase client)
    {
        try
        {
            if (client is AmneziaWgClient awgClient)
            {
                var awg = VpnManager.GetType<AmneziaWg>();

                var allAwgStats = awg.GetOnlineStats(true);
                return allAwgStats.FirstOrDefault(s => s.ClientId == awgClient.PublicKey);
            }

            if (client is WireGuardClient wgClient)
            {
                var wg = VpnManager.GetType<WireGuard>();

                var allWgStats = wg.GetOnlineStats(true);
                return allWgStats.FirstOrDefault(s => s.ClientId == wgClient.PublicKey);
            }

            if (client is VlessClient vlessClient)
            {
                var xray = VpnManager.GetType<Xray>();

                var allXrayStats = xray.GetOnlineStats(true);
                return allXrayStats.FirstOrDefault(s => s.ClientId.Equals(vlessClient.Name, StringComparison.OrdinalIgnoreCase));
            }
        }
        catch (Exception ex)
        {
            Logger.Warn($"Failed to fetch online stats for client {client.Name}: {ex.Message}");
        }

        return null;
    }
}