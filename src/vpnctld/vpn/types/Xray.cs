using System.Text;

public class Xray : IVpnService
{
    private readonly string _basePath = "/usr/local/etc/xray/";

    public VpnServiceType Type => VpnServiceType.XRAY;

    public void Init()
    {
        GenerateRealityKeys();
        GenerateDefaultUuid();
    }

    public bool Install()
    {
        var cmd = Kernel.Get<ICommandRunner>();

        cmd.Run("apt-get", "install curl unzip iptables -y", true, false);

        if (!Directory.Exists(_basePath))
        {
            Directory.CreateDirectory(_basePath);
            Logger.Info($"Environment configuration directory created: \"{_basePath}\"");
        }

        Logger.Info("Downloading and executing official XTLS deployment script from GitHub...");
        var curlRes = cmd.Run("curl", "-L https://github.com/XTLS/Xray-install/raw/main/install-release.sh -o /tmp/install-release.sh", true, true);
        if (!curlRes.Success)
            throw new Exception("Failed to download XTLS installer script.");

        var bashRes = cmd.Run("bash", "/tmp/install-release.sh install", true, true);
        if (!bashRes.Success)
            throw new Exception("XTLS deployment script returned an execution failure code.");

        Logger.Info("Xray-core engine binaries deployed successfully.");

        return true;
    }

    public bool Uninstall()
    {
        var cmd = Kernel.Get<ICommandRunner>();

        cmd.Run("systemctl", "stop xray", true, false);
        cmd.Run("systemctl", "disable xray", true, false);
        cmd.Run("killall", "xray", true, false);

        if (!File.Exists("/tmp/install-release.sh"))
        {
            cmd.Run("curl", "-L https://github.com -o /tmp/install-release.sh", true, false);
        }

        Logger.Info("Invoking official Xray uninstaller script...");

        var uninstallResult = cmd.Run("bash", "/tmp/install-release.sh remove --purge", true, true);
        if (uninstallResult.Success)
        {
            Logger.Success("Xray-core engine components purged successfully via script.");
        }
        else
        {
            Logger.Warn("Uninstaller script failed. Executing manual system purge...");
            cmd.Run("rm", "-f /usr/local/bin/xray /usr/bin/xray /etc/systemd/system/xray.service", true, false);
            cmd.Run("systemctl", "daemon-reload", true, false);
        }

        if (Directory.Exists(_basePath))
        {
            try
            {
                Logger.Info($"Purging configuration directory: {_basePath}...");
                Directory.Delete(_basePath, true);
                Logger.Info("Configuration environment directory deleted successfully.");
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to delete system configuration directory: {ex.Message}");
            }
        }

        if (File.Exists("/tmp/install-release.sh"))
        {
            File.Delete("/tmp/install-release.sh");
        }

        return true;
    }

    public bool GenerateRealityKeys()
    {
        var data = Kernel.Get<IDataProvider>();
        var server = data.GetServerState();
        var cmd = Kernel.Get<ICommandRunner>();

        var keyGenResult = cmd.Run("xray", "x25519", true, false);
        if (!keyGenResult.Success || string.IsNullOrWhiteSpace(keyGenResult.Text))
            throw new Exception($"Xray key generation failed: {keyGenResult.Text}");

        var keys = TextParser.ParseRealityKeyCommand(keyGenResult.Text);
        if (string.IsNullOrEmpty(keys.publicKey) || string.IsNullOrEmpty(keys.privateKey))
        {
            throw new Exception($"Failed to extract Reality keys.");
        }

        server.Xray.Vless.Reality.PrivateKey = keys.privateKey;
        server.Xray.Vless.Reality.PublicKey = keys.publicKey;

        return true;
    }

    public bool GenerateDefaultUuid()
    {
        var data = Kernel.Get<IDataProvider>();
        var server = data.GetServerState();

        server.Xray.Vless.DefaultUuid = Guid.NewGuid().ToString();
        return true;
    }

    public bool Restart()
    {
        UpdateSystemConfig();

        var cmd = Kernel.Get<ICommandRunner>();

        var restartResult = cmd.Run("systemctl", "restart xray", true, false);
        if (!restartResult.Success)
            throw new Exception($"Failed to restart Xray-core service! {restartResult.Text}");

        return true;
    }

    public bool ToggleActive(bool active)
    {
        var systemctlAction = active ? "start" : "stop";

        var cmd = Kernel.Get<ICommandRunner>();

        var result = cmd.Run("systemctl", $"{systemctlAction} xray", true, true);
        if (!result.Success)
        {
            throw new Exception($"Failed to toggle Xray service! {result.Text}");
        }

        return true;
    }

    public VpnClientBase? CreateVlessClient(string name, bool needShortId)
    {
        var data = Kernel.Get<IDataProvider>();

        var server = data.GetServerState();
        var serverIp = Kernel.Get<INetworkManager>().GetIP();

        var clientUuid = Guid.NewGuid().ToString();

        if (string.IsNullOrEmpty(server.Xray.Vless.DefaultUuid))
        {
            throw new Exception("Vless DefaultUuid missing from storage.");
        }

        if (string.IsNullOrEmpty(server.Xray.Vless.Sni))
        {
            throw new Exception("Vless sni cant be empty.");
        }

        var security = VpnHelper.ParseXraySecurity(server.Xray.Vless.Security);
        if (security == XraySecurity.REALITY)
        {
            if (string.IsNullOrEmpty(server.Xray.Vless.Reality.PrivateKey) || string.IsNullOrEmpty(server.Xray.Vless.Reality.PublicKey))
            {
                throw new Exception("Reality keys missing from storage.");
            }
        }
        else if (security == XraySecurity.TLS)
        {

        }

        string shortId = string.Empty;
        if (needShortId && security == XraySecurity.REALITY)
        {
            shortId = Guid.NewGuid().ToString("n").Substring(0, 16);
        }

        var vlessUrl = ConfigFormatBuilder.GetVlessClientUrl(server, clientUuid, name, shortId, serverIp);
        return new VlessClient
        {
            Name = name,
            Uuid = clientUuid,
            ConfigStr = vlessUrl,
            ShortId = shortId
        };
    }

    public VpnClientBase? CreateSocksClient(string name, string? customPassword = null)
    {
        var data = Kernel.Get<IDataProvider>();

        var serverData = data.GetServerState();
        var serverIp = Kernel.Get<INetworkManager>().GetIP();

        var password = !string.IsNullOrEmpty(customPassword) ? customPassword : Guid.NewGuid().ToString("N")[..12];

        return new SocksClient
        {
            Name = name,
            Password = password,
            ConfigStr = ConfigFormatBuilder.GetSocksClientUrl(serverData, name, password, serverIp),
        };
    }

    public VpnClientBase? CreateSsClient(string name)
    {
        var data = Kernel.Get<IDataProvider>();

        var serverData = data.GetServerState();
        var serverIp = Kernel.Get<INetworkManager>().GetIP();

        var method = "aes-128-gcm";

        var randomBytes = System.Security.Cryptography.RandomNumberGenerator.GetBytes(16);
        var password = Convert.ToBase64String(randomBytes);

        var ssUrl = ConfigFormatBuilder.GetShadowsocksClientUrl(serverData, name, password, method, serverIp);

        return new ShadowsocksClient
        {
            Name = name,
            Password = password,
            Method = method,
            ConfigStr = ssUrl
        };
    }

    public string GetInfo()
    {
        var cmd = Kernel.Get<ICommandRunner>();

        var result = cmd.Run("systemctl", "status xray --no-pager", false, false);
        if (result.Success) return result.Text.Trim();

        return string.Empty;
    }

    public string GetLogs(int lines)
    {
        var result = Kernel.Get<ICommandRunner>().Run("journalctl", $"-u xray -n {lines} --no-pager", true, false);
        return result.Text.Trim();
    }

    public List<ClientOnlineStats> GetOnlineStats()
    {
        var result = new List<ClientOnlineStats>();

        return result;
    }

    public VpnInstallStatus GetInstallStatus()
    {
        var cmd = Kernel.Get<ICommandRunner>();

        var result = cmd.Run("xray", "version", false, false);
        return result.Success ? VpnInstallStatus.INSTALLED : VpnInstallStatus.NOT_INSTALLED;
    }

    public VpnActiveStatus GetActiveStatus()
    {
        if (GetInstallStatus() == VpnInstallStatus.NOT_INSTALLED)
            return VpnActiveStatus.INACTIVE;

        var cmd = Kernel.Get<ICommandRunner>();

        var result = cmd.Run("pidof", "xray", false, false);
        if (result.Success && !string.IsNullOrWhiteSpace(result.Text))
        {
            return VpnActiveStatus.ACTIVE;
        }

        return VpnActiveStatus.INACTIVE;
    }

    private void UpdateSystemConfig()
    {
        var data = Kernel.Get<IDataProvider>();

        var clients = data.GetClientsState();
        var server = data.GetServerState();

        var fullConfig = ConfigFormatBuilder.GetXrayServerConfig(server, clients);
        var path = Path.Combine(_basePath, "config.json");

        Kernel.Get<IFileManager>().TrySave(path, fullConfig);
    }
}

public enum XraySecurity
{
    NONE,
    TLS,
    REALITY,
}