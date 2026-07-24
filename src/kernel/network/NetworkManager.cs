public class NetworkManager : INetworkManager
{
    private static string? _cachedIp = null;

    public void EnableIPv4Forwarding()
    {
        File.WriteAllText("/etc/sysctl.d/vpnctl.conf","net.ipv4.ip_forward = 1\n");
        Kernel.Cmd.Run("sysctl", "-p /etc/sysctl.d/vpnctl.conf", true, false);
    }

    public void EnableIPv6Forwarding()
    {

    }

    public string GetIP()
    {
        if (_cachedIp != null) return _cachedIp;

        var cmd = Kernel.Cmd;

        var ipResult = cmd.Run("curl", "-s --connect-timeout 5 https://ifconfig.me", false, false);
        if (!ipResult.Success || string.IsNullOrWhiteSpace(ipResult.Text))
        {
            Logger.Warn($"Cannot get server ip via curl: {ipResult.Text}");

            var fallbackIp = Kernel.Data.GetServerState().ServerIpFallback;

            if (string.IsNullOrWhiteSpace(fallbackIp))
            {
                throw new Exception("IP resolution failed: External network service is unreachable, and 'ServerIpFallback' is not defined in config.json.");
            }

            Logger.Info($"Using configured fallback IP: {fallbackIp}");
            return fallbackIp;
        }

        return _cachedIp = ipResult.Text.Trim();
    }
}