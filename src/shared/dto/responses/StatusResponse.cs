public class StatusResponse
{
    public List<VpnNetData> Vpns { get; set; } = new();
    public SystemNetData System { get; set; } = new();

    public string ServerIp { get; set; } = string.Empty;
    public string NetworkInterface { get; set; } = string.Empty;
}