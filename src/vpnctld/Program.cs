var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

Kernel.Register<ICommandRunner>(() => new LinuxCommandRunner());
Kernel.Register<IDataProvider>(() => new JsonDataProvider());
Kernel.Register<IFirewallManager>(() => new UfwFirewallManager());
Kernel.Register<INetworkManager>(() => new NetworkManager());
Kernel.Register<ISystemConfigurator>(() => new SystemConfigurator());
Kernel.Register<IFileManager>(() => new BaseFileManager());
Kernel.Register<ISystemMonitor>(() => new BaseSystemMonitor());

VpnManager.Register(() => new WireGuard());
VpnManager.Register(() => new AmneziaWg());
VpnManager.Register(() => new Xray());

app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        Logger.Error(ex.ToString());

        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";

        var response = new ApiResponse<object>
        {
            Error = ex.Message
        };

        await context.Response.WriteAsJsonAsync(response);
    }
    finally
    {
        if (Kernel.IsCreated<IDataProvider>())
        {
            Kernel.Data.TrySave();
        }
    }
});

app.MapGet(ApiRoutes.Status, HandleStatus);
app.MapGet(ApiRoutes.VpnLogs, HandleLogs);
app.MapGet(ApiRoutes.Clients, HandleClients);

app.MapPost(ApiRoutes.VpnAction, HandleVpnAction);
app.MapPost(ApiRoutes.Purge, HandlePurge);
app.MapPost(ApiRoutes.ClientAction, HandleClientAction);
app.MapPost(ApiRoutes.ProtocolAction, HandleProtocolAction);

app.Run(Kernel.Data.GetServerState().Adress);

IResult HandleProtocolAction(ProtocolActionRequest request)
{
    var proto = request.Type;
    var value = request.Value;
    var action = request.Action;

    if (proto == ProtocolType.WIREGUARD)
    {
        var wg = VpnManager.Wg;

        if (action == ProtocolNetActionType.GEN_KEYS)
        {
            wg.GenerateKeys();
        }
        else
        {
            return ApiResult.Bad($"Wireguard not supported: {action.ToString()}.");
        }
    }
    else if (proto == ProtocolType.AMNEZIAWG)
    {
        var awg = VpnManager.Awg;

        if (action == ProtocolNetActionType.GEN_KEYS)
        {
            awg.GenerateServerKeys();
        }
        else if (action == ProtocolNetActionType.OBFUSCATE)
        {
            awg.GenerateObfuscation();
        }
        else if (action == ProtocolNetActionType.RANDOMIZE_PORT)
        {
            awg.RandomizePort();
        }
        else
        {
            return ApiResult.Bad($"AmneziaWG not supported: {action.ToString()}.");
        }
    }
    else if (proto == ProtocolType.VLESS)
    {
        var xray = VpnManager.Xray;
        var data = Kernel.Data.GetServerState().Xray;

        if (action == ProtocolNetActionType.GEN_KEYS)
        {
            xray.GenerateRealityKeys();
        }
        else if (action == ProtocolNetActionType.GEN_UUID)
        {
            xray.GenerateDefaultUuid();
        }
        else if (action == ProtocolNetActionType.SET_FINGERPRINT)
        {
            if (string.IsNullOrEmpty(value))
                return ApiResult.Bad($"Fingerprint cant be empty.");

            data.Vless.Fingerprint = value;
        }
        else if (action == ProtocolNetActionType.SET_SNI)
        {
            if (string.IsNullOrEmpty(value))
                return ApiResult.Bad($"Sni cant be empty.");

            data.Vless.Sni = value;
        }
        else if (action == ProtocolNetActionType.SET_SECURITY)
        {
            if (string.IsNullOrEmpty(value))
                return ApiResult.Bad($"Security cant be empty.");

            if (value != "none" && value != "reality" && value != "tls")
            {
                return ApiResult.Bad($"Unsupported security type {value}.");
            }

            data.Vless.Security = value;
        }
        else
        {
            return ApiResult.Bad($"Xray not supported: {action.ToString()}.");
        }
    }
    else
    {
        return ApiResult.Bad($"{proto.ToString()} not supported.");
    }

    return ApiResult.Ok();
}

IResult HandleClients(string? name)
{
    var clients = Kernel.Data.GetClientsState();

    if (clients.Clients.Count == 0)
    {
        return ApiResult.Ok(new ClientsResponse());
    }

    var response = new ClientsResponse();

    if (!string.IsNullOrEmpty(name))
    {
        var client = Kernel.Data.GetClient(name);
        if (client == null)
        {
            return ApiResult.Bad($"Client '{name}' not found");
        }

        response.Clients.Add(
            client.ToNet(
                ClientHelper.GetProtocolFromClientType(client),
                ClientHelper.GetOnlineStatsForClient(client)
            ));
    }
    else
    {
        var onlineStats = ClientHelper.GetOnlineStats();

        foreach (var client in clients.Clients)
        {
            response.Clients.Add(
            client.ToNet(
                ClientHelper.GetProtocolFromClientType(client),
                ClientHelper.GetOnlineStatsForClient(client, onlineStats)
            ));
        }
    }

    return ApiResult.Ok(response);
}

IResult HandleClientAction(ClientActionRequest request)
{
    var response = new ClientNetData();
    var clientsState = Kernel.Data.GetClientsState();

    switch (request.Action)
    {
        case ClientNetActionType.ADD:
            if (request.Protocol == null)
                return ApiResult.Bad("Protocol required for add");

            var nextId = clientsState.LastClientId + 1;

            var clientName = string.IsNullOrWhiteSpace(request.Name)
            ? $"client_{nextId}"
            : request.Name;

            if (Kernel.Data.IsNameExist(clientName))
                return ApiResult.Bad("Client name already exists");

            VpnClientBase? client = null;
            IVpnService? vpn = null;

            if (request.Protocol == ProtocolType.WIREGUARD)
            {
                var wg = VpnManager.GetType<WireGuard>();
                client = wg.CreateClient(clientName);
                vpn = wg;
            }
            else if (request.Protocol == ProtocolType.AMNEZIAWG)
            {
                var awg = VpnManager.GetType<AmneziaWg>();
                client = awg.CreateClient(clientName);
                vpn = awg;
            }
            else if (request.Protocol == ProtocolType.VLESS ||
                request.Protocol == ProtocolType.SOCKS ||
                request.Protocol == ProtocolType.SHADOWSOCKS)
            {
                var xray = VpnManager.GetType<Xray>();

                if (request.Protocol == ProtocolType.VLESS)
                {
                    client = xray.CreateVlessClient(clientName, request.NeedShortId);
                }
                else if (request.Protocol == ProtocolType.SOCKS)
                {
                    client = xray.CreateSocksClient(clientName, request.Password);
                }
                else if (request.Protocol == ProtocolType.SHADOWSOCKS)
                {
                    client = xray.CreateSsClient(clientName);
                }

                vpn = xray;
            }

            if (client != null)
            {
                clientsState.LastClientId++;
                client.Id = clientsState.LastClientId;

                clientsState.Clients.Add(client);
                vpn?.Restart();

                response = client.ToNet(request.Protocol.Value);
            }

            break;
        case ClientNetActionType.DEL:
            if (string.IsNullOrEmpty(request.Name))
                return ApiResult.Bad("Name required for del");

            var clientToDelete = clientsState.Clients.FirstOrDefault(c => c.Name == request.Name);
            if (clientToDelete == null)
            {
                return ApiResult.Bad($"Name {request.Name} not exist!");
            }

            clientsState.Clients.Remove(clientToDelete);

            if (clientToDelete is WireGuardClient)
            {
                var wg = VpnManager.GetType<WireGuard>();
                wg.Restart();

                Logger.Success($"WireGuard client \"{request.Name}\" deleted.");
            }
            else if (clientToDelete is AmneziaWgClient)
            {
                var awg = VpnManager.GetType<AmneziaWg>();
                awg.Restart();

                Logger.Success($"AmneziaWG client \"{request.Name}\" deleted.");
            }
            else if (clientToDelete is VlessClient || clientToDelete is SocksClient || clientToDelete is ShadowsocksClient)
            {
                var xray = VpnManager.GetType<Xray>();
                xray.Restart();

                Logger.Success($"Xray client \"{request.Name}\" deleted.");
            }
            else
            {
                var unknownType = clientToDelete.GetType().Name;
                Logger.Warn($"Client \"{request.Name}\" was removed, but its service type \"{unknownType}\" is unhandled.");
            }
            break;
        case ClientNetActionType.DOWN:
        case ClientNetActionType.UP:
            if (string.IsNullOrEmpty(request.Name))
                return ApiResult.Bad("Name required for up/down");

            var clientToActive = clientsState.Clients.FirstOrDefault(c => c.Name == request.Name);
            if (clientToActive == null)
            {
                return ApiResult.Bad($"Name {request.Name} not exist!");
            }

            clientToActive.IsActive = request.Action == ClientNetActionType.UP ? true : false;

            if (clientToActive is WireGuardClient)
            {
                var wg = VpnManager.GetType<WireGuard>();
                wg.Restart();
            }
            else if (clientToActive is AmneziaWgClient)
            {
                var awg = VpnManager.GetType<AmneziaWg>();
                awg.Restart();
            }
            else if (clientToActive is VlessClient || clientToActive is SocksClient || clientToActive is ShadowsocksClient)
            {
                var xray = VpnManager.GetType<Xray>();
                xray.Restart();
            }
            else
            {
                var unknownType = clientToActive.GetType().Name;
            }

            break;
    }

    return ApiResult.Ok(response);
}

IResult HandleLogs(int lines)
{
    var vpns = VpnManager.GetAll();

    var response = new VpnLogsResponse();

    foreach (var vpn in vpns)
    {
        response.Vpns.Add(new VpnLogsNetData
        {
            Type = vpn.Type,
            LogsLines = vpn.GetLogs(lines).Split("\n", StringSplitOptions.RemoveEmptyEntries).ToList(),
        });
    }

    return ApiResult.Ok(response);
}

IResult HandlePurge()
{
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
        Logger.Info("Reverting sysctl..");

        Kernel.System.DeleteSysctlConfig();
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

    return ApiResult.Ok();
}

IResult HandleVpnAction(VpnActionRequest request)
{
    var vpn = VpnManager.Get(request.Type);
    if (vpn == null)
    {
        return ApiResult.Bad("Unsupported VPN type");
    }

    switch (request.Action)
    {
        case VpnNetActionType.INSTALL:
            if (vpn.GetInstallStatus() == VpnInstallStatus.INSTALLED)
            {
                return ApiResult.Bad("VPN already installed");
            }

            vpn.Install();
            vpn.Restart();
            break;

        case VpnNetActionType.UNINSTALL:
            if (vpn.GetInstallStatus() == VpnInstallStatus.NOT_INSTALLED)
            {
                return ApiResult.Bad("VPN already uninstalled");
            }

            vpn.Uninstall();
            break;

        case VpnNetActionType.RESTART:
            if (vpn.GetInstallStatus() == VpnInstallStatus.NOT_INSTALLED)
            {
                return ApiResult.Bad("VPN uninstalled");
            }

            vpn.Restart();
            break;

        case VpnNetActionType.INIT:
            if (vpn.GetInstallStatus() == VpnInstallStatus.NOT_INSTALLED)
            {
                return ApiResult.Bad("VPN uninstalled");
            }

            vpn.Init();
            vpn.Restart();
            break;

        case VpnNetActionType.UP:
            if (vpn.GetInstallStatus() == VpnInstallStatus.NOT_INSTALLED)
            {
                return ApiResult.Bad("VPN uninstalled");
            }

            if (vpn.GetActiveStatus() == VpnActiveStatus.ACTIVE)
            {
                return ApiResult.Bad("VPN already active");
            }

            vpn.ToggleActive(true);
            break;

        case VpnNetActionType.DOWN:
            if (vpn.GetInstallStatus() == VpnInstallStatus.NOT_INSTALLED)
            {
                return ApiResult.Bad("VPN uninstalled");
            }

            if (vpn.GetActiveStatus() == VpnActiveStatus.INACTIVE)
            {
                return ApiResult.Bad("VPN already inactive");
            }

            vpn.ToggleActive(false);
            break;

        default:
            return ApiResult.Bad("Unsupported action");
    }

    return ApiResult.Ok();
}

IResult HandleStatus()
{
    var vpns = VpnManager.GetAll();

    var data = Kernel.Data;
    var server = data.GetServerState();
    var clients = data.GetClientsState().Clients;

    var response = new StatusResponse();

    List<int> GetPorts(VpnServiceType type)
    {
        var result = new List<int>();
        switch (type)
        {
            case VpnServiceType.WIREGUARD:
                result.Add(server.Wg.Port);
                break;
            case VpnServiceType.AMNEZIAWG:
                result.Add(server.Awg.Port);
                break;
            case VpnServiceType.XRAY:
                result.Add(server.Xray.Vless.Port);
                result.Add(server.Xray.Socks.Port);
                result.Add(server.Xray.Shadowsocks.Port);
                break;
        }

        return result;
    }

    int GetTotalClients(VpnServiceType type)
    {
        return type switch
        {
            VpnServiceType.WIREGUARD => clients.Count(c => c is WireGuardClient),
            VpnServiceType.AMNEZIAWG => clients.Count(c => c is AmneziaWgClient),
            VpnServiceType.XRAY => clients.Count(c => c is VlessClient || c is SocksClient || c is ShadowsocksClient),
            _ => 0
        };
    }

    foreach (var vpn in vpns)
    {
        var stats = vpn.GetOnlineStats();

        response.Vpns.Add(new VpnNetData
        {
            Type = vpn.Type,
            Installed = vpn.GetInstallStatus(),
            Active = vpn.GetActiveStatus(),
            Clients = GetTotalClients(vpn.Type),
            OnlineClients = stats.Count(s =>
                s.LastConnectAt.HasValue &&
                (DateTime.UtcNow - s.LastConnectAt.Value).TotalMinutes < 3),
            Ports = GetPorts(vpn.Type),
            BytesReceived = stats.Sum(c => c.BytesRecived),
            BytesSent = stats.Sum(c => c.BytesSent),
        });
    }

    var system = Kernel.Monitor.GetStats();

    response.System = new SystemNetData
    {
        CpuUsage = system.CpuUsage,
        TotalMemory = system.TotalMemory,
        UsageMemory = system.UsageMemory,
        LoadAverage = system.LoadAverage,
        Uptime = system.Uptime,
    };

    response.ServerIp = Kernel.Network.GetIP();
    response.NetworkInterface = data.GetServerState().NetworkInterface;

    return ApiResult.Ok(response);
}