public class WireGuard : IVpnService
{
    public VpnServiceType Type => VpnServiceType.WIREGUARD;

    public void Init()
    {
        GenerateKeys();
    }

    public bool Install()
    {
        Kernel.Get<IFileManager>().CreateDirectories(PathRegistry.WgDir);

        var cmd = Kernel.Get<ICommandRunner>();

        Logger.Info("Installing WireGuard binary components via package manager...");
        var result = cmd.Run("apt-get", "install wireguard iptables -y", true, false);
        if (!result.Success)
        {
            throw new Exception($"WireGuard core package install failed! Error: {result.Text}");
        }

        Logger.Info("Initializing internal interface environment setup...");

        return true;
    }

    public bool Uninstall()
    {
        ToggleActive(false);

        var cmd = Kernel.Get<ICommandRunner>();

        var result = cmd.Run("apt", "purge wireguard wireguard-tools -y", true);
        if (!result.Success)
        {
            throw new Exception($"WireGuard uninstall failed! {result.Text}");
        }

        Kernel.Get<IFileManager>().Delete(PathRegistry.WgDir);
        return true;
    }

    public bool GenerateKeys()
    {
        var data = Kernel.Get<IDataProvider>();

        var serverData = data.GetServerState();
        var cmd = Kernel.Get<ICommandRunner>();

        var privResult = cmd.Run("wg", "genkey", true, false);
        if (!privResult.Success)
        {
            throw new Exception($"Failed create private key for WireGuard {privResult.Text}");
        }
        serverData.Wg.PrivateKey = privResult.Text.Trim();

        Logger.Info("Success creating private key for server");

        var pubResult = cmd.Run("bash", $"-c \"echo '{serverData.Wg.PrivateKey}' | wg pubkey\"", true, false);
        if (!pubResult.Success)
        {
            throw new Exception($"Failed create public key for WireGuard {pubResult.Text}");
        }
        serverData.Wg.PublicKey = pubResult.Text.Trim();

        Logger.Info("Success creating public key for server");
        return true;
    }

    public bool Restart()
    {
        var wg = Kernel.Get<IDataProvider>().GetServerState().Wg;
        var cmd = Kernel.Get<ICommandRunner>();

        UpdateSystemConfig();

        var reloadResult = cmd.Run("systemctl", $"reload wg-quick@{wg.InterfaceName}", true, false);
        if (!reloadResult.Success)
        {
            var restartResult = cmd.Run("systemctl", $"restart wg-quick@{wg.InterfaceName}", true, false);
            if (!restartResult.Success)
                throw new Exception($"Failed to restart Wireguard service! {restartResult.Text}");
        }

        return true;
    }

    public bool ToggleActive(bool active)
    {
        var wg = Kernel.Get<IDataProvider>().GetServerState().Wg;
        var action = active ? "start" : "stop";

        var result = Kernel.Get<ICommandRunner>().Run("systemctl", $"{action} wg-quick@{wg.InterfaceName}", true, false);
        if (!result.Success)
            throw new Exception($"Failed {action} Wireguard. {result.Text.Trim()}");

        return true;
    }

    public VpnClientBase? CreateClient(string name)
    {
        var data = Kernel.Get<IDataProvider>();

        var clients = data.GetClientsState();
        var wgClientsCount = clients.Clients.Count(c => c is WireGuardClient);

        var nextId = clients.LastClientId + 1;
        var allowedIp = IpAllocator.GetNextIp("10.0", nextId);

        var cmd = Kernel.Get<ICommandRunner>();

        var privResult = cmd.Run("wg", "genkey", true, false);
        if (!privResult.Success)
            throw new Exception($"Cannot create private key: {privResult.Text}");

        var clientPrivateKey = privResult.Text.Trim();
        Logger.Info("Success creating private key for client");

        var pubResult = cmd.Run("bash", $"-c \"echo '{clientPrivateKey}' | wg pubkey\"", true, false);
        if (!pubResult.Success)
            throw new Exception($"Cannot create public key: {pubResult.Text}");

        var serverData = data.GetServerState();

        var clientPublicKey = pubResult.Text.Trim();
        Logger.Info("Success creating public key for client");

        if (string.IsNullOrEmpty(serverData.Wg.PublicKey))
            throw new Exception($"Public key for WireGuard not found.");

        var serverIp = Kernel.Get<INetworkManager>().GetIP();
        if (string.IsNullOrEmpty(serverIp))
            return null;

        if (!Kernel.Get<IFileManager>().Exists(PathRegistry.GetWgConf(serverData.Wg.InterfaceName)))
            throw new Exception($"File \"{serverData.Wg.InterfaceName}.conf\" not found");

        var serverPrivateKey = GetPrivateKey();
        if (string.IsNullOrEmpty(serverPrivateKey))
            return null;

        var clientConfig = ConfigFormatBuilder.GetWgClientConfString(serverData, serverIp, allowedIp, clientPrivateKey, serverData.Wg.PublicKey);
        clientConfig = clientConfig.Trim();

        return new WireGuardClient()
        {
            Name = name,
            AllowedIp = allowedIp,
            PrivateKey = clientPrivateKey,
            PublicKey = clientPublicKey,
            ConfigStr = clientConfig,
        };
    }

    public string GetInfo()
    {
        var cmd = Kernel.Get<ICommandRunner>();

        var result = cmd.Run("wg", "show", false, false);
        if (result.Success) return result.Text.Trim();

        return string.Empty;
    }

    public string GetLogs(int lines)
    {
        var wg = Kernel.Get<IDataProvider>().GetServerState().Wg;

        var result = Kernel.Get<ICommandRunner>().Run("journalctl", $"-u wg-quick@{wg.InterfaceName} -n {lines} --no-pager", true, false);
        return result.Text.Trim();
    }

    public List<ClientOnlineStats> GetOnlineStats()
    {
        var data = Kernel.Get<IDataProvider>();
        var wg = data.GetServerState().Wg;

        var dump = Kernel.Get<ICommandRunner>().Run("wg", $"show {wg.InterfaceName} dump", true, false);
        if (!dump.Success)
        {
            Logger.Warn($"Failed to get dump for interface \"{wg.InterfaceName}\". {dump.Text.Trim()}");
            return new List<ClientOnlineStats>();
        }

        var result = TextParser.GetAwgOrWgOnlineStats(dump.Text);
        return result;
    }

    public VpnInstallStatus GetInstallStatus()
    {
        var cmd = Kernel.Get<ICommandRunner>();

        var result = cmd.Run("wg", "--version", false, false);
        return result.Success ? VpnInstallStatus.INSTALLED : VpnInstallStatus.NOT_INSTALLED;
    }

    public VpnActiveStatus GetActiveStatus()
    {
        if (GetInstallStatus() == VpnInstallStatus.NOT_INSTALLED)
            return VpnActiveStatus.INACTIVE;

        var wg = Kernel.Get<IDataProvider>().GetServerState().Wg;

        return Kernel.Get<INetworkManager>().InterfaceExists(wg.InterfaceName) ? VpnActiveStatus.ACTIVE : VpnActiveStatus.INACTIVE;
    }

    private void UpdateSystemConfig()
    {
        var data = Kernel.Get<IDataProvider>();

        var clients = data.GetClientsState();
        var server = data.GetServerState();

        var fullConfig = ConfigFormatBuilder.GetWgServerConfString(server, clients);

        Kernel.Get<IFileManager>().TrySave(PathRegistry.GetWgConf(server.Wg.InterfaceName), fullConfig);
    }

    private string GetPrivateKey()
    {
        var data = Kernel.Get<IDataProvider>();

        var server = data.GetServerState();
        if (string.IsNullOrEmpty(server.Wg.PrivateKey))
            throw new Exception($"Private key for WireGuard not found!");

        return server.Wg.PrivateKey;
    }

    private bool SetPeer(string publicKey, string ip)
    {
        //var serverCfg = Server.GetConfig();

        //var result = _cmd.Run("wg", $"set {serverCfg.WgInterfaceName} peer {publicKey} allowed-ips {ip}/32", true);
        //if (!result.Success)
        //{
        //    Console.WriteLine($"Failed to set new peer: {ip}, Error: {result.Text}");
        //    return false;
        //}

        return true;
    }

    private bool RemovePeer(string publicKey)
    {
        //var serverCfg = Server.GetConfig();

        //var result = _cmd.Run("wg", $"set {serverCfg.WgInterfaceName} peer {publicKey} remove", true);
        //if (!result.Success)
        //{
        //    Console.WriteLine($"Failed to remove peer, Error: {result.Text}");
        //    return false;
        //}

        return true;
    }
}