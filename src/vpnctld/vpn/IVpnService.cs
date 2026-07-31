public interface IVpnService
{
    VpnServiceType Type { get; }

    void Init();
    bool Install();
    bool Uninstall();
    bool Restart();
    bool ToggleActive(bool active);

    VpnInstallStatus GetInstallStatus();
    VpnActiveStatus GetActiveStatus();

    string GetInfo();
    string GetLogs(int lines = 30);
    List<ClientOnlineStats> GetOnlineStats();
}