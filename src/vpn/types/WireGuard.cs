public class WireGuard : IVpnService
{
    private readonly string _basePath = "/etc/wireguard/";

    public VpnServiceType Type => VpnServiceType.WIREGUARD;

    private List<ClientOnlineStats>? _cachedStats = null;

    public bool Install(bool isForce)
    {
        if (!Directory.Exists(_basePath))
        {
            Directory.CreateDirectory(_basePath);
            Logger.Info($"Creating configuration core environment directory: \"{_basePath}\"");
        }

        var cmd = Kernel.Cmd;

        Logger.Info("Installing WireGuard binary components via package manager...");
        var result = cmd.Run("apt-get", "install wireguard iptables -y", true, false);
        if (!result.Success)
        {
            throw new Exception($"WireGuard core package install failed! Error: {result.Text}");
        }

        Logger.Info("Initializing internal interface environment setup...");

        GenerateKeys(isForce);
        return true;
    }

    public bool Uninstall()
    {
        ToggleActive(false);

        var cmd = Kernel.Cmd;

        var result = cmd.Run("apt", "purge wireguard wireguard-tools -y", true);
        if (!result.Success)
        {
            throw new Exception($"WireGuard uninstall failed! {result.Text}");
        }

        if (Directory.Exists(_basePath))
        {
            try
            {
                Logger.Info($"Cleaning up directory {_basePath}...");

                Directory.Delete(_basePath, true);

                Logger.Info("Directory cleaned up successfully!");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to delete directory! {ex.Message}");
            }
        }

        return true;
    }

    public bool GenerateKeys(bool isForce)
    {
        var data = Kernel.Data;

        var serverData = data.GetServerState();
        var cmd = Kernel.Cmd;

        if (!isForce)
            if (!VpnHelper.CanReplaceWgKeys(serverData)) return false;

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
        var wg = Kernel.Data.GetServerState().Wg;
        var cmd = Kernel.Cmd;

        UpdateSystemConfig();

        var reloadResult = cmd.Run("systemctl", $"reload wg-quick@{wg.InterfaceName}", true, false);
        if (!reloadResult.Success)
        {
            var restartResult = cmd.Run("systemctl", $"restart wg-quick@{wg.InterfaceName}", true, false);
            if (!restartResult.Success)
                throw new Exception($"Failed to restart WireGuard service! {restartResult.Text}");
        }

        return true;
    }

    public bool ToggleActive(bool active)
    {
        var data = Kernel.Data;

        var serverData = data.GetServerState();
        var cmd = Kernel.Cmd;

        var systemctlAction = active ? "start" : "stop";

        var result = cmd.Run("systemctl", $"{systemctlAction} wg-quick@{serverData.Wg.InterfaceName}", true, false);
        if (!result.Success)
        {
            throw new Exception($"Failed to toggle WireGuard service! {result.Text}");
        }

        return true;
    }

    public VpnClientBase? CreateClient(string name)
    {
        var data = Kernel.Data;

        var clients = data.GetClientsState();
        var wgClientsCount = clients.Clients.Count(c => c is WireGuardClient);

        int nextIpNode = wgClientsCount + 2;
        if (nextIpNode > 254)
            throw new Exception("Failed create client: Subnet is full (maximum 253 clients)");

        var generatedClientIp = $"10.0.0.{nextIpNode}";

        var cmd = Kernel.Cmd;

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

        var serverIp = Kernel.Network.GetIP();
        if (string.IsNullOrEmpty(serverIp))
            return null;

        if (!File.Exists(_basePath + $"/{serverData.Wg.InterfaceName}.conf"))
            throw new Exception($"File \"{serverData.Wg.InterfaceName}.conf\" not found");

        var serverPrivateKey = GetPrivateKey();
        if (string.IsNullOrEmpty(serverPrivateKey))
            return null;

        var clientConfig = FormatManager.GetWgClientConfString(serverData, serverIp, generatedClientIp, clientPrivateKey, serverData.Wg.PublicKey);
        clientConfig = clientConfig.Trim();

        return new WireGuardClient()
        {
            Name = name,
            AllowedIp = generatedClientIp,
            PrivateKey = clientPrivateKey,
            PublicKey = clientPublicKey,
            ConfigStr = clientConfig,
        };
    }

    public string GetInfo()
    {
        var cmd = Kernel.Cmd;

        var result = cmd.Run("wg", "show", false, false);
        if (result.Success) return result.Text.Trim();

        return string.Empty;
    }

    public string GetLogs(int lines)
    {
        var wg = Kernel.Data.GetServerState().Wg;

        var result = Kernel.Cmd.Run("journalctl", $"-u wg-quick@{wg.InterfaceName} -n {lines} --no-pager", true, false);
        return result.Text.Trim();
    }

    public List<ClientOnlineStats> GetOnlineStats()
    {
        if (_cachedStats != null) return _cachedStats;

        var data = Kernel.Data;
        var wg = data.GetServerState().Wg;

        var dump = Kernel.Cmd.Run("wg", $"show {wg.InterfaceName} dump", true, false);
        if (!dump.Success)
        {
            //Logger.Warn($"Failed to get dump for interface \"{wg.InterfaceName}\". {dump.Text.Trim()}");
            return new List<ClientOnlineStats>();
        }

        var result = FormatManager.GetAwgOrWgOnlineStats(dump.Text);
        return _cachedStats = result;
    }

    public VpnInstallStatus GetInstallStatus()
    {
        var cmd = Kernel.Cmd;

        var result = cmd.Run("wg", "--version", false, false);
        return result.Success ? VpnInstallStatus.INSTALLED : VpnInstallStatus.NOT_INSTALLED;
    }

    public VpnActiveStatus GetActiveStatus()
    {
        if (GetInstallStatus() == VpnInstallStatus.NOT_INSTALLED)
            return VpnActiveStatus.INACTIVE;

        return Directory.Exists("/sys/class/net/wg0") ? VpnActiveStatus.ACTIVE : VpnActiveStatus.INACTIVE;
    }

    private void UpdateSystemConfig()
    {
        var data = Kernel.Data;

        var clients = data.GetClientsState();
        var server = data.GetServerState();

        var fullConfig = FormatManager.GetWgServerConfString(server, clients);
        var path = Path.Combine(_basePath, $"{server.Wg.InterfaceName}.conf");

        Kernel.File.TrySaveFile(path, fullConfig);
    }

    private string GetPrivateKey()
    {
        var data = Kernel.Data;

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