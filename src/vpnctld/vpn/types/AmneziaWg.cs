using System.Security.Cryptography;

public class AmneziaWg : IVpnService
{
    public VpnServiceType Type => VpnServiceType.AMNEZIAWG;

    public void Init()
    {
        RandomizePort();
        GenerateServerKeys();
        GenerateObfuscation();
    }

    public bool Install()
    {
        Kernel.Get<IFileManager>().CreateDirectories(PathRegistry.AwgDir);

        var cmd = Kernel.Get<ICommandRunner>();

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

        return true;
    }

    public bool Uninstall()
    {
        ToggleActive(false);

        var cmd = Kernel.Get<ICommandRunner>();
        var awg = Kernel.Get<IDataProvider>().GetServerState().Awg;

        Logger.Info("Purging AmneziaWG package from system...");

        cmd.Run("apt-get", "purge amneziawg amneziawg-dkms amneziawg-tools -y", true, false);

        Kernel.Get<IFileManager>().Delete(PathRegistry.AwgDir);

        var port = awg.Port;
        Kernel.Get<IFirewallManager>().CloseUdp(port);

        return true;
    }

    public bool Restart()
    {
        var awg = Kernel.Get<IDataProvider>().GetServerState().Awg;
        var cmd = Kernel.Get<ICommandRunner>();

        RegenerateConfig();

        var reloadResult = cmd.Run("systemctl", $"reload awg-quick@{awg.InterfaceName}", true, false);
        if (!reloadResult.Success)
        {
            var restartResult = cmd.Run("systemctl", $"restart awg-quick@{awg.InterfaceName}", true, false);
            if (!restartResult.Success)
                throw new Exception($"Failed to restart AmneziaWG service! {restartResult.Text}");
        }

        return true;
    }

    public VpnClientBase? CreateClient(string name)
    {
        var cmd = Kernel.Get<ICommandRunner>();

        var server = Kernel.Get<IDataProvider>().GetServerState();
        var serverIp = Kernel.Get<INetworkManager>().GetIP();

        var privResult = cmd.Run("awg", "genkey", true, false);
        string clientPriv = privResult.Text.Trim();

        var pubResult = cmd.Run("bash", $"-c \"echo '{clientPriv}' | awg pubkey\"", true, false);
        string clientPub = pubResult.Text.Trim();

        var clientsState = Kernel.Get<IDataProvider>().GetClientsState();

        var nextId = clientsState.LastClientId + 1;
        var allowedIp = IpAllocator.GetNextIp("10.8", nextId);

        string clientConfigText = ConfigFormatBuilder.GetAWgClientConfig(server, allowedIp, clientPriv, serverIp);

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
        var awg = Kernel.Get<IDataProvider>().GetServerState().Awg;
        var action = active ? "start" : "stop";

        var result = Kernel.Get<ICommandRunner>().Run("systemctl", $"{action} awg-quick@{awg.InterfaceName}", true, false);
        if (!result.Success)
            throw new Exception($"Failed {action} AmneziaWG. {result.Text.Trim()}");

        return result.Success;
    }

    public string GetInfo()
    {
        var result = Kernel.Get<ICommandRunner>().Run("awg", "show", false, false);
        if (result.Success) return result.Text.Trim();

        return string.Empty;
    }

    public string GetLogs(int lines)
    {
        var awg = Kernel.Get<IDataProvider>().GetServerState().Awg;

        var result = Kernel.Get<ICommandRunner>().Run("journalctl", $"-u awg-quick@{awg.InterfaceName} -n {lines} --no-pager", true, false);
        return result.Text.Trim();
    }

    public List<ClientOnlineStats> GetOnlineStats()
    {
        var data = Kernel.Get<IDataProvider>();
        var awg = data.GetServerState().Awg;

        var dump = Kernel.Get<ICommandRunner>().Run("awg", $"show {awg.InterfaceName} dump", true, false);
        if (!dump.Success)
        {
            Logger.Warn($"Failed to get dump for interface \"{awg.InterfaceName}\". {dump.Text.Trim()}");
            return new List<ClientOnlineStats>();
        }

        var result = TextParser.GetAwgOrWgOnlineStats(dump.Text);
        return result;
    }

    public VpnInstallStatus GetInstallStatus()
    {
        var result = Kernel.Get<ICommandRunner>().Run("awg", "--version", false, false);
        return result.Success ? VpnInstallStatus.INSTALLED : VpnInstallStatus.NOT_INSTALLED;
    }

    public VpnActiveStatus GetActiveStatus()
    {
        if (GetInstallStatus() == VpnInstallStatus.NOT_INSTALLED)
            return VpnActiveStatus.INACTIVE;

        var awg = Kernel.Get<IDataProvider>().GetServerState().Awg;

        return Kernel.Get<INetworkManager>().InterfaceExists(awg.InterfaceName) ? VpnActiveStatus.ACTIVE : VpnActiveStatus.INACTIVE;
    }

    public bool GenerateServerKeys()
    {
        var server = Kernel.Get<IDataProvider>().GetServerState();
        var cmd = Kernel.Get<ICommandRunner>();

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

        var awg = Kernel.Get<IDataProvider>().GetServerState().Awg;

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
        var awg = Kernel.Get<IDataProvider>().GetServerState().Awg;
        var firewall = Kernel.Get<IFirewallManager>();

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
        var data = Kernel.Get<IDataProvider>();

        var server = data.GetServerState();
        var clients = data.GetClientsState();

        var fullConfig = ConfigFormatBuilder.BuildAwgServerConfig(server, clients);

        Kernel.Get<IFileManager>().TrySave(PathRegistry.GetAwgConf(server.Awg.InterfaceName), fullConfig);
    }
}