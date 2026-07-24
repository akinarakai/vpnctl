public class ClientsShowHandler : IHandler
{
    public bool CanHandle(InputContext input)
    {
        // client name/list (flag=qr)
        if (input.Count > 3) return false;

        return input.Args[0] == "client";
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
        var useQr = input.HasFlag<QrFlag>();

        if (isSingle)
        {
            var name = input.Args[1];

            var client = data.GetClient(name);
            if (client == null)
            {
                Logger.Warn($"Cannot found client: {name}.");
                return;
            }

            PrintConfig(client, useQr);
        }
        else
        {
            for (int i = 0; i < clients.Count; i++)
            {
                var client = clients[i];
                PrintConfig(client, useQr, i + 1);
            }
        }
    }

    private void PrintConfig(VpnClientBase client, bool useQr, int? iter = null)
    {
        var providerStr = "Unknown";
        var dateStr = client.CreatedAt.ToLocalTime().ToString("dd.MM.yyyy");

        if (client is WireGuardClient)
        {
            providerStr = "WireGuard";
        }
        else if (client is AmneziaWgClient)
        {
            providerStr = "AmneziaWG";
        }
        else if (client is VlessClient)
        {
            providerStr = "VLESS";
        }
        else if (client is SocksClient)
        {
            providerStr = "SOCKS";
        }
        else if (client is ShadowsocksClient)
        {
            providerStr = "Shadowsocks";
        }

        var interStr = iter != null ? $"{iter}. " : "";
        Console.WriteLine($"{interStr}{client.Name} | {providerStr} | {dateStr}");

        if (!string.IsNullOrEmpty(client.ConfigStr))
        {
            Console.WriteLine($"--- CONFIG ---");
            if (useQr)
            {
                QRCode.Render(client.ConfigStr);
            }
            else
            {
                Console.WriteLine(client.ConfigStr);
            }

            Console.WriteLine("-------------------------------------");
            Console.WriteLine();
        }
    }
}