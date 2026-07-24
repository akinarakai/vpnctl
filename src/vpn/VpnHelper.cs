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

    public static bool CanReplaceRealityKeys(ServerData serverData)
    {
        if (!string.IsNullOrEmpty(serverData.Xray.Vless.Reality.PrivateKey) && !string.IsNullOrEmpty(serverData.Xray.Vless.Reality.PublicKey))
        {
            return GetSuccessInput("Xray Reality");
        }

        return true;
    }

    public static bool CanReplaceWgKeys(ServerData serverData)
    {
        if (!string.IsNullOrEmpty(serverData.Wg.PrivateKey) && !string.IsNullOrEmpty(serverData.Wg.PublicKey))
        {
            return GetSuccessInput("WireGuard");
        }

        return true;
    }

    private static bool GetSuccessInput(string name)
    {
        Logger.Warn($"Cryptographic keys for {name} already exist!");
        Logger.Warn("Are you sure you want to overwrite the current keypair? (Y/N):");

        var answer = Console.ReadLine()?.Trim().ToLower();
        if (answer != "y")
        {
            Logger.Info($"Action aborted. Retaining the existing operational keys for {name}.");
            return false;
        }

        return true;
    }
}