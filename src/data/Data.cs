public class WireGuardData
{
    public int Port { get; set; } = 51820;
    public string PrivateKey { get; set; } = string.Empty;
    public string PublicKey { get; set; } = string.Empty;
    public string BaseIp { get; set; } = "10.0.0.1/24";
    public string Subnet { get; set; } = "10.0.0.0/24";
    public string InterfaceName { get; set; } = "wg0";
}

public class AmneziaWgData
{
    public int Port { get; set; } = 36117;
    public string PrivateKey { get; set; } = string.Empty;
    public string PublicKey { get; set; } = string.Empty;
    public string BaseIp { get; set; } = "10.8.0.1/24";
    public string Subnet { get; set; } = "10.8.0.0/24";
    public string InterfaceName { get; set; } = "awg0";

    public int Jc { get; set; }
    public int Jmin { get; set; }
    public int Jmax { get; set; }
    public int S1 { get; set; }
    public int S2 { get; set; }
    public int S3 { get; set; }
    public int S4 { get; set; }
    public string H1 { get; set; } = string.Empty;
    public string H2 { get; set; } = string.Empty;
    public string H3 { get; set; } = string.Empty;
    public string H4 { get; set; } = string.Empty;
    public string I1 { get; set; } = string.Empty;
}

public class RealityData
{
    public string PrivateKey { get; set; } = string.Empty;
    public string PublicKey { get; set; } = string.Empty;
}

public class VlessData
{
    public string DefaultUuid { get; set; } = string.Empty;
    public int Port { get; set; } = 443;
    public string Flow { get; set; } = "xtls-rprx-vision";
    public string Security { get; set; } = "reality"; // reality/tls/none
    public string Sni { get; set; } = "www.microsoft.com";
    public string Fingerprint { get; set; } = "chrome";

    public RealityData Reality { get; init; } = new();
}

public class SocksData
{
    public int Port { get; set; } = 1080;
}

public class ShadowsocksData
{
    public int Port { get; set; } = 8388;
}

public class XrayData
{
    public string LogLevel { get; init; } = "debug";

    public VlessData Vless { get; init; } = new();
    public SocksData Socks { get; init; } = new();
    public ShadowsocksData Shadowsocks { get; init; } = new();
}

public class ClientsData
{
    public long LastClientId { get; set; } = 0;
    public List<VpnClientBase> Clients { get; set; } = new();
}

public class ServerData
{
    public string ServerIpFallback { get; set; } = string.Empty;
    public string NetworkInterface { get; set; } = "eth0";

    public SysctlConfig SysctlConfig { get; set; } = new();

    public WireGuardData Wg { get; set; } = new();
    public AmneziaWgData Awg { get; set; } = new();
    public XrayData Xray { get; set; } = new();
}