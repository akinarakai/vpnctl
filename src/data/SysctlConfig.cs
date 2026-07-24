public class SysctlConfig
{
    public bool IpV4Forwarding { get; set; } = true;
    public bool IpV6Forwarding { get; set; } = true;

    public string CongestionControl { get; set; } = "bbr";
    public int MaxConnectionsBacklog { get; set; } = 4096;

    public bool DisableIcmpEchoIgnoreAll { get; set; } = true; 
    public int RpFilter { get; set; } = 1; 
}