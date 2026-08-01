public static class VpnHelper
{
    private static XraySecurity? _cachedSecurity = null;

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