public class ClientActionHandler : IHandler
{
    private static readonly HashSet<string> ReservedNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "add", "del", "list", "show", "remove", "client"
    };

    public bool CanHandle(InputContext input)
    {
        // proto client add name // 4 args
        // client del name // 3 args

        var isCreate = input.Count >= 4 &&
                    input.Args[1] == "client" &&
                    input.Args[2] == "add";

        var isDelete = input.Count >= 3 &&
                        input.Args[0] == "client" &&
                        input.Args[1] == "del";

        return isCreate || isDelete;
    }

    public void Handle(InputContext input)
    {
        var data = Kernel.Data;

        var action = input.Args[2];
        var clients = data.GetClientsState();

        if (action == "add")
        {
            HandleCreate(input, clients);
        }
        else
        {
            HandleDelete(input, clients);
        }
    }

    private void HandleCreate(InputContext input, ClientsData clientsData)
    {
        var clientName = input.Args[3];

        if (ReservedNames.Contains(clientName))
        {
            Logger.Warn($"Failed to create client: '{clientName}' is a reserved command keyword.");
            return;
        }

        var data = Kernel.Data;

        if (data.IsNameExist(clientName))
        {
            Logger.Warn($"Failed create client: name {clientName} exist!");
            return;
        }

        VpnClientBase? client = null;
        IVpnService? vpn = null;

        var protoName = input.Args[0];
        if (protoName == "wg")
        {
            var wg = VpnManager.GetType<WireGuard>();
            client = wg.CreateClient(clientName);

            vpn = wg;
        }
        else if (protoName == "awg")
        {
            var awg = VpnManager.GetType<AmneziaWg>();
            client = awg.CreateClient(clientName);

            vpn = awg;
        }
        else if (protoName == "vless" || protoName == "socks" || protoName == "ss")
        {
            if (Enum.TryParse<XrayProtoType>(protoName, true, out var proto))
            {
                var xray = VpnManager.GetType<Xray>();

                if (proto == XrayProtoType.VLESS)
                {
                    var needShortId = input.HasFlag<ShortIdFlag>();
                    client = xray.CreateVlessClient(clientName, needShortId);
                }
                else if (proto == XrayProtoType.SOCKS)
                {
                    string? password = null;
                    if (input.TryGetFlag<PasswordFlag>(out var pwdFlag) && pwdFlag?.Arguments?.Count > 0)
                    {
                        password = pwdFlag.Arguments[0];
                    }

                    client = xray.CreateSocksClient(clientName, password);
                }
                else if (proto == XrayProtoType.SS)
                    client = xray.CreateSsClient(clientName);

                vpn = xray;
            }
        }

        if (client != null)
        {
            clientsData.Clients.Add(client);
            vpn?.Restart();

            Logger.Text("--- CONFIG ---");
            Logger.Text(client.ConfigStr);
            QRCode.Render(client.ConfigStr);
            Logger.Text("-------------------------------------");
        }
        else
        {
            Logger.Warn($"Failed create client");
        }
    }

    private void HandleDelete(InputContext input, ClientsData clientsData)
    {
        var clientName = input.Args[2];
        var clientToDelete = clientsData.Clients.FirstOrDefault(c => c.Name == clientName);
        if (clientToDelete == null)
        {
            Logger.Warn($"Failed delete: name {clientName} not exist!");
            return;
        }

        clientsData.Clients.Remove(clientToDelete);

        if (clientToDelete is WireGuardClient)
        {
            var wg = VpnManager.GetType<WireGuard>();
            wg.Restart();

            Logger.Success($"WireGuard client \"{clientName}\" deleted.");
        }
        else if (clientToDelete is AmneziaWgClient)
        {
            var awg = VpnManager.GetType<AmneziaWg>();
            awg.Restart();

            Logger.Success($"AmneziaWG client \"{clientName}\" deleted.");
        }
        else if (clientToDelete is VlessClient || clientToDelete is SocksClient || clientToDelete is ShadowsocksClient)
        {
            var xray = VpnManager.GetType<Xray>();
            xray.Restart();

            Logger.Success($"Xray client \"{clientName}\" deleted.");
        }
        else
        {
            var unknownType = clientToDelete.GetType().Name;
            Logger.Warn($"Client \"{clientName}\" was removed, but its service type \"{unknownType}\" is unhandled.");
        }
    }
}