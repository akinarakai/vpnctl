public class ServerInfoResponse
{
    public string Ip { get; set; } = "";
    public string NetworkInterface { get; set; } = "";
    public string Hostname { get; set; } = "";
    public string Os { get; set; } = "";
    public string Arch { get; set; } = "";

    public DateTime UtcTime { get; set; }
    public TimeSpan Uptime { get; set; }
}