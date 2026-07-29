public static class PathRegistry
{
    public static string AppDir => AppDomain.CurrentDomain.BaseDirectory;
    public static string VpnctlDir => "/etc/vpnctl";

    public const string SysctlConf = "/etc/sysctl.d/99-vpnctl.conf";

    public const string WgDir = "/etc/wireguard";
    public const string AwgDir = "/etc/amnezia/amneziawg";

    public static string GetConfFile(string path, string interfaceName)
    {
        return Path.Combine(path, $"{interfaceName}.conf");
    }

    public static string GetWgConf(string interfaceName)
    {
        return GetConfFile(WgDir, interfaceName);
    }

    public static string GetAwgConf(string interfaceName)
    {
        return GetConfFile(AwgDir, interfaceName);
    }
}