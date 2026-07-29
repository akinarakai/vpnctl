public enum VpnServiceType : byte
{
    WIREGUARD = 0,
    AMNEZIAWG,
    XRAY,
}

public enum VpnInstallStatus : byte
{
    NOT_INSTALLED = 0,
    INSTALLED,
}

public enum VpnActiveStatus : byte
{
    ACTIVE = 0,
    INACTIVE,
}

public interface IVpnService
{
    VpnServiceType Type { get; }

    bool Install(bool force);
    bool Uninstall();
    bool Restart();
    bool ToggleActive(bool active);

    VpnInstallStatus GetInstallStatus();
    VpnActiveStatus GetActiveStatus();

    string GetInfo();
    string GetLogs(int lines = 30);
    List<ClientOnlineStats> GetOnlineStats(bool useCache = true);
}