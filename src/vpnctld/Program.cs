var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();

var app = builder.Build();

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

app.Use(async (context, next) =>
{
    try
    {
        if (!context.Request.Headers.TryGetValue("Authorization", out var authorization))
        {
            await ApiResult.Unauthorized(context);
            return;
        }

        const string Prefix = "Bearer ";

        var header = authorization.ToString();
        if (!header.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
        {
            await ApiResult.Unauthorized(context);
            return;
        }

        var token = header.Substring(Prefix.Length);

        var authToken = Kernel.Get<IDataProvider>().GetToken(token);
        if (authToken == null)
        {
            await ApiResult.Unauthorized(context);
            return;
        }

        authToken.LastUsedAt = DateTime.UtcNow;

        context.SetAuthToken(authToken);
        await next();
    }
    catch (Exception ex)
    {
        await ApiResult.Error(context, ex.ToString());
    }
    finally
    {
        if (Kernel.IsCreated<IDataProvider>())
        {
            Kernel.Get<IDataProvider>().TrySave();
        }
    }
});


// GET
app.MapGet(ApiRoutes.Server.Info, HandleServerInfo);
app.MapGet(ApiRoutes.System.Monitor, HandleSystemMonitor);

app.MapGet(ApiRoutes.Vpn.List, HandleVpnList);
app.MapGet(ApiRoutes.Vpn.Logs, HandleVpnLogs);

app.MapGet(ApiRoutes.Tokens.List, HandleTokenList);

app.MapGet(ApiRoutes.Clients.List, HandleClients);

// POST
app.MapPost(ApiRoutes.Vpn.Action, HandleVpnAction);

app.MapPost(ApiRoutes.Clients.Action, HandleClientAction);

app.MapPost(ApiRoutes.Protocols.Action, HandleProtocolAction);

app.MapPost(ApiRoutes.Tokens.Action, HandleTokenActions);

app.MapPost(ApiRoutes.Maintenance.Purge, HandlePurge);

var data = Kernel.Get<IDataProvider>();

Logger.Info($"vpntld started with {data.GetTokens().Count} tokens.");

app.Run($"http://{data.GetServerState().ListenAddress}:{data.GetServerState().ListenPort}");

IResult HandleTokenList()
{
    var tokens = Kernel.Get<IDataProvider>().GetTokens();

    return ApiResult.Ok(new AuthTokenListResponse
    {
        Tokens = tokens.Select(t => t.ToNet()).ToList()
    });
}

IResult HandleTokenActions(AuthTokenActionRequest request)
{
    var data = Kernel.Get<IDataProvider>();

    var existing = data.GetToken(request.Name);

    if (request.Action == AuthTokenNetAction.ADD)
    {
        if (existing != null)
            return ApiResult.Bad($"Token with name '{request.Name}' already exists.");

        if (request.Name.StartsWith("auto-", StringComparison.OrdinalIgnoreCase))
            return ApiResult.Bad("Reserved token name.");

        if (request.AccessLevel == null)
            return ApiResult.Bad("Access level required for add.");

        data.AddToken(request.Name, request.AccessLevel.Value, out var secret);

        return ApiResult.Ok(new AuthTokenResponse
        {
            Secret = secret,
        });
    }
    else if (request.Action == AuthTokenNetAction.DEL)
    {
        if (existing == null)
            return ApiResult.NotFound($"Token with name '{request.Name}' not found.");

        data.RemoveToken(request.Name);

        return ApiResult.Ok();
    }

    return ApiResult.Bad($"Unsupported action type {request.Action}.");
}

IResult HandleSystemMonitor()
{
    var system = Kernel.Get<ISystemMonitor>().GetStats();
    var response = new SystemMonitorResponse()
    {
        CpuUsage = system.CpuUsage,
        TotalMemory = system.TotalMemory,
        UsageMemory = system.UsageMemory,
        LoadAverage = system.LoadAverage,
        Uptime = system.Uptime,
    };

    return ApiResult.Ok(response);
}

IResult HandleServerInfo()
{
    var network = Kernel.Get<INetworkManager>();

    var response = new ServerInfoResponse
    {
        Ip = network.GetIP(),
        NetworkInterface = network.GetActiveNetInterface(),
        Hostname = Environment.MachineName,
        Os = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
        Arch = System.Runtime.InteropServices.RuntimeInformation.OSArchitecture.ToString(),
        UtcTime = DateTime.UtcNow,
        Uptime = Kernel.Get<ISystemMonitor>().GetUptime(Kernel.Get<ICommandRunner>()),
    };

    return ApiResult.Ok(response);
}

IResult HandleVpnList(VpnServiceType? type)
{
    var data = Kernel.Get<IDataProvider>();
    var server = data.GetServerState();
    var clients = data.GetClientsState().Clients;

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

    VpnNetData BuildVpnData(IVpnService vpn)
    {
        var stats = vpn.GetOnlineStats();

        return new VpnNetData
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
            BytesSent = stats.Sum(c => c.BytesSent)
        };
    }

    var response = new VpnListResponse();

    if (type != null)
    {
        var vpn = VpnManager.Get(type.Value);
        if (vpn == null)
            return ApiResult.Bad($"Unsupported vpn type: {type}");

        response.Vpns.Add(BuildVpnData(vpn));
        return ApiResult.Ok(response);
    }

    var vpns = VpnManager.GetAll();

    foreach (var vpn in vpns)
    {
        var stats = vpn.GetOnlineStats();

        response.Vpns.Add(BuildVpnData(vpn));
    }

    return ApiResult.Ok(response);
}

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
        var data = Kernel.Get<IDataProvider>().GetServerState().Xray;

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
    var data = Kernel.Get<IDataProvider>();

    var clients = data.GetClientsState();

    if (clients.Clients.Count == 0)
    {
        return ApiResult.Ok(new ClientsResponse());
    }

    var response = new ClientsResponse();

    if (!string.IsNullOrEmpty(name))
    {
        var client = data.GetClient(name);
        if (client == null)
        {
            return ApiResult.NotFound($"Client '{name}' not found");
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

    var data = Kernel.Get<IDataProvider>();
    var clientsState = data.GetClientsState();

    switch (request.Action)
    {
        case ClientNetActionType.ADD:
            if (request.Protocol == null)
                return ApiResult.Bad("Protocol required for add");

            var nextId = clientsState.LastClientId + 1;

            var clientName = string.IsNullOrWhiteSpace(request.Name)
            ? $"client_{nextId}"
            : request.Name;

            if (data.GetClient(clientName) != null)
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

IResult HandleVpnLogs(int lines)
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

        Kernel.Get<ISystemConfigurator>().DeleteSysctlConfig();
    }
    catch (Exception ex)
    {
        Logger.Error($"Failed to delete system configuration: {ex.Message}");
    }

    // Storage
    try
    {
        Logger.Info("Deleting application configuration files...");
        Kernel.Get<IDataProvider>().DeleteFiles();
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