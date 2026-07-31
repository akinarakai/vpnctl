public static class VpnHelper
{
    private static XraySecurity? _cachedSecurity = null;

    public static int GetFreePort(int exclude, int min, int max)
    {
        var server = Kernel.Data.GetServerState();
        for (int port = min; port <= max; port++)
        {
            if (port == exclude) continue;

            var result = Kernel.Cmd.Run("ss", $"-H -lntu '( sport = :{port} )'");
            if (string.IsNullOrWhiteSpace(result.Text))
                return port;
        }

        throw new Exception();
    }

    public static string GetNameFromType(VpnServiceType type)
    {
        return type switch
        {
            VpnServiceType.WIREGUARD => "WireGuard",
            VpnServiceType.AMNEZIAWG => "AmneziaWG",
            VpnServiceType.XRAY => "Xray",
            _ => throw new ArgumentOutOfRangeException(nameof(type), $"Неизвестный тип провайдера: {type}")
        };
    }

    public static XraySecurity ParseXraySecurity(string securityStr)
    {
        if (_cachedSecurity != null) return _cachedSecurity.Value;

        switch (securityStr)
        {
            case "none":
                _cachedSecurity = XraySecurity.NONE;
                break;
            case "reality":
                _cachedSecurity = XraySecurity.REALITY;
                break;
            case "tls":
                _cachedSecurity = XraySecurity.TLS;
                break;
            default:
                throw new ArgumentOutOfRangeException($"Unknown xray sercurity type {securityStr}");
        }

        return _cachedSecurity.Value;
    }
}