using System.Security.Cryptography;

public class AmneziaWg : IVpnService
{
    private readonly string _basePath = "/etc/amnezia/amneziawg/";

    public VpnServiceType Type => VpnServiceType.AMNEZIAWG;

    private List<ClientOnlineStats>? _cachedStats = null;

    public bool Install(bool force)
    {
        var cmd = Kernel.Cmd;

        var deps = cmd.Run("apt-get", "install software-properties-common python3-launchpadlib curl iptables -y", true);
        if (!deps.Success)
            throw new Exception("Failed installing dependencies");

        Logger.Info("Adding AmneziaWG repository...");

        cmd.Run("add-apt-repository", "-y ppa:amnezia/ppa", true);
        cmd.Run("apt-get", "update", true);

        Logger.Info("Downloading and deploying AmneziaWG kernel module...");

        var install = cmd.Run("apt", "-y install dkms linux-headers-generic iptables nftables amneziawg amneziawg-tools -y", true);
        if (!install.Success || GetInstallStatus() == VpnInstallStatus.NOT_INSTALLED)
            throw new Exception("AmneziaWG package installation failed.");

        RandomizePort();
        GenerateServerKeys(force);
        GenerateObfuscation();

        return true;
    }

    public bool Uninstall()
    {
        var cmd = Kernel.Cmd;
        var awg = Kernel.Data.GetServerState().Awg;

        cmd.Run("systemctl", $"stop awg-quick@{awg.InterfaceName}", true, false);
        cmd.Run("systemctl", $"disable awg-quick@{awg.InterfaceName}", true, false);

        Logger.Info("Purging AmneziaWG package from system...");
        cmd.Run("apt-get", "purge amneziawg amneziawg-dkms amneziawg-tools -y", true, false);
        cmd.Run("apt-get", "autoremove -y", true, false);

        if (Directory.Exists(_basePath))
        {
            Directory.Delete(_basePath, true);
        }

        var port = awg.Port;
        Kernel.Firewall.CloseUdp(port);

        return true;
    }

    public bool Restart()
    {
        RegenerateConfig();

        var cmd = Kernel.Cmd;
        var awg = Kernel.Data.GetServerState().Awg;

        cmd.Run("systemctl", $"restart awg-quick@{awg.InterfaceName}", true, false);
        return true;
    }

    public VpnClientBase? CreateClient(string name)
    {
        var cmd = Kernel.Cmd;

        var server = Kernel.Data.GetServerState();
        var serverIp = Kernel.Network.GetIP();

        var privResult = cmd.Run("awg", "genkey", true, false);
        string clientPriv = privResult.Text.Trim();

        var pubResult = cmd.Run("bash", $"-c \"echo '{clientPriv}' | awg pubkey\"", true, false);
        string clientPub = pubResult.Text.Trim();

        var clientsState = Kernel.Data.GetClientsState();
        int currentClientCount = clientsState.Clients.OfType<AmneziaWgClient>().Count();

        string allowedIp = $"10.8.0.{currentClientCount + 2}";

        string clientConfigText = FormatManager.GetAWgClientConfig(server, allowedIp, clientPriv, serverIp);

        return new AmneziaWgClient
        {
            Name = name,
            PrivateKey = clientPriv,
            PublicKey = clientPub,
            ConfigStr = clientConfigText,
            AllowedIp = allowedIp,
        };
    }

    public bool ToggleActive(bool active)
    {
        var awg = Kernel.Data.GetServerState().Awg;
        var action = active ? "start" : "stop";

        var result = Kernel.Cmd.Run("systemctl", $"{action} awg-quick@{awg.InterfaceName}", true, false);
        if (!result.Success)
            throw new Exception($"Failed {action} AmneziaWG. {result.Text.Trim()}");

        return result.Success;
    }

    public string GetInfo()
    {
        var result = Kernel.Cmd.Run("awg", "show", false, false);
        if (result.Success) return result.Text.Trim();

        return string.Empty;
    }

    public List<ClientOnlineStats> GetOnlineStats()
    {
        if (_cachedStats != null) return _cachedStats;

        var result = new List<ClientOnlineStats>();

        var data = Kernel.Data;
        var awg = data.GetServerState().Awg;

        var dump = Kernel.Cmd.Run("awg", $"show {awg.InterfaceName} dump", true, false);
        if (!dump.Success)
        {
            Logger.Warn($"Failed to get dump for interface \"{awg.InterfaceName}\". {dump.Text.Trim()}");
            return result;
        }

        var lines = dump.Text.Trim().Split('\n', StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            var parts = line.Split('\t');

            if (parts.Length < 7) continue;
            if (parts.Length > 10) continue;

            // parts[0] - awg0
            // parts[1] - public key
            // parts[2] - preshared-key
            // parts[3] - endpoint
            // parts[4] - allowed-ips
            // parts[5] - last handshake (unix timestamp)
            // parts[6] - bytes received
            // parts[7] - bytes transmitted

            var publicKey = parts[0].Trim();
            var endPoint = parts[2].Trim();

            if (endPoint == "none" || endPoint == "(none)")
            {
                endPoint = null;
            }

            DateTime? lastConnectAt = null;
            if (long.TryParse(parts[4].Trim(), out long unixTime) && unixTime > 0)
            {
                lastConnectAt = DateTimeOffset.FromUnixTimeSeconds(unixTime).UtcDateTime;
            }

            long.TryParse(parts[5].Trim(), out long rxBytes);
            long.TryParse(parts[6].Trim(), out long txBytes);

            var client = new ClientOnlineStats
            {
                ClientId = publicKey,
                Endpoint = endPoint,
                LastConnectAt = lastConnectAt,
                BytesRecived = txBytes,
                BytesSent = rxBytes
            };

            //System.Console.WriteLine(client.ToString());

            result.Add(client);
        }

        return _cachedStats = result;
    }

    public VpnInstallStatus GetInstallStatus()
    {
        var result = Kernel.Cmd.Run("awg", "--version", false, false);
        return result.Success ? VpnInstallStatus.INSTALLED : VpnInstallStatus.NOT_INSTALLED;
    }

    public VpnActiveStatus GetActiveStatus()
    {
        if (GetInstallStatus() == VpnInstallStatus.NOT_INSTALLED)
            return VpnActiveStatus.INACTIVE;

        var awg = Kernel.Data.GetServerState().Awg;
        var sysNetPath = $"/sys/class/net/{awg.InterfaceName}";

        return Directory.Exists(sysNetPath) ? VpnActiveStatus.ACTIVE : VpnActiveStatus.INACTIVE;
    }

    public bool GenerateServerKeys(bool force)
    {
        var server = Kernel.Data.GetServerState();
        var cmd = Kernel.Cmd;

        var privResult = cmd.Run("awg", "genkey", true, false);
        string privateKey = privResult.Text.Trim();

        var pubResult = cmd.Run("bash", $"-c \"echo '{privateKey}' | awg pubkey\"", true, false);
        string publicKey = pubResult.Text.Trim();

        server.Awg.PrivateKey = privateKey;
        server.Awg.PublicKey = publicKey;
        return true;
    }

    public bool GenerateObfuscation()
    {
        /*
        awg.Jc = 3;
        awg.Jmin = 43;
        awg.Jmax = 217;

        awg.S1 = 150;
        awg.S2 = 84;
        awg.S3 = 38;
        awg.S4 = 18;

        awg.H1 = $"15485589-443089163";
        awg.H2 = $"708138339-863784626";
        awg.H3 = $"929145294-1058155196";
        awg.H4 = $"1105336284-1753689410";

        awg.I1 = "<r 210>";
        */

        /*
        string GenerateHRange(int min, int max)
        {
            int start = RandomNumberGenerator.GetInt32(min, max);
            int end = RandomNumberGenerator.GetInt32(start, max);

            return $"{start}-{end}";
        }
        */

        var awg = Kernel.Data.GetServerState().Awg;

        awg.Jc = RandomNumberGenerator.GetInt32(3, 5);
        awg.Jmin = RandomNumberGenerator.GetInt32(40, 80);
        awg.Jmax = RandomNumberGenerator.GetInt32(150, 250);

        //awg.Jc = 3;
        //awg.Jmin = 43;
        //awg.Jmax = 217;

        awg.S1 = 150;
        awg.S2 = 84;
        awg.S3 = 38;
        awg.S4 = 18;

        //awg.H1 = GenerateHRange(10000000, 500000000);
        //awg.H2 = GenerateHRange(600000000, 1000000000);
        //awg.H3 = GenerateHRange(1100000000, 1500000000);
        //awg.H4 = GenerateHRange(1600000000, 2000000000);

        awg.H1 = $"15485589-443089163";
        awg.H2 = $"708138339-863784626";
        awg.H3 = $"929145294-1058155196";
        awg.H4 = $"1105336284-1753689410";

        awg.I1 = "<r 210>";

        return true;
    }

    public bool RandomizePort()
    {
        // 39743
        var awg = Kernel.Data.GetServerState().Awg;
        var firewall = Kernel.Firewall;

        var oldPort = awg.Port;
        var newPort = RandomNumberGenerator.GetInt32(30000, 45000);

        if (oldPort == newPort) return true;

        if (!firewall.OpenUdp(newPort))
        {
            Logger.Error($"Failed to open UDP port {newPort}");
            return false;
        }

        if (oldPort > 0)
        {
            firewall.CloseUdp(oldPort);
        }

        awg.Port = newPort;
        return true;
    }

    private void RegenerateConfig()
    {
        var data = Kernel.Data;

        var server = data.GetServerState();
        var clients = data.GetClientsState();

        var fullConfig = FormatManager.BuildAwgServerConfig(server, clients);
        var targetPath = Path.Combine(_basePath, $"{server.Awg.InterfaceName}.conf");

        Kernel.File.TrySaveFile(targetPath, fullConfig);
    }
}