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
        var isSingle = input.Args[1] != "list";
        var response = ApiClient.Get().GetClients(isSingle ? input.Args[1] : null);
        if (response.Clients.Count == 0)
        {
            Logger.Warn("No clients found.");
            return;
        }

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
            PrintConfig(response.Clients[0], displayMode);
        }
        else
        {
            for (int i = 0; i < response.Clients.Count; i++)
            {
                var client = response.Clients[i];
                PrintConfig(client, displayMode, i + 1);
            }
        }
    }

    private void PrintConfig(ClientNetData client, ConfigDisplayMode displayMode, int? iter = null)
    {
        var providerStr = FormatManager.GetProtocolNameFromType(client.Protocol);
        var activeStr = client.IsActive ? "● ENABLED" : "○ BLOCKED";

        var dateStr = client.CreatedAt.ToLocalTime().ToString("dd.MM.yyyy");
        var interStr = iter != null ? $"{iter}. " : "";

        var networkStatusStr = "○ OFFLINE";
        if (client.Stats != null && client.Stats.LastConnectAt.HasValue)
        {
            var timeSinceLastConnect = DateTime.UtcNow - client.Stats.LastConnectAt.Value;
            if (timeSinceLastConnect.TotalMinutes <= 3)
            {
                networkStatusStr = "● ONLINE";
            }
        }

        Console.WriteLine($"{interStr}{client.Name} ({client.Id}) | {providerStr} | {activeStr} | {networkStatusStr} | Created: {dateStr}");

        if (client.Stats != null)
        {
            var lastConnectStr = client.Stats.LastConnectAt.HasValue
                ? FormatManager.GetRelativeTime(client.Stats.LastConnectAt.Value)
                : "Never";

            var endpointStr = string.IsNullOrEmpty(client.Stats.Endpoint) ? "None" : client.Stats.Endpoint;

            var (downStr, upStr) = FormatManager.FormatTraffic(client.Stats.BytesRecived, client.Stats.BytesSent);

            Console.WriteLine($"  -> Endpoint: {endpointStr}");
            Console.WriteLine($"  -> Last Activity: {lastConnectStr}");
            Console.WriteLine($"  -> Traffic: Download: {downStr} | Upload: {upStr}");
        }
        else
        {
            Console.WriteLine("  -> Statistics: No data available");
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
}