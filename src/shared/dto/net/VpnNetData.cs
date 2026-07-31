public class VpnNetData
{
    public VpnServiceType Type { get; init; }
    public VpnInstallStatus Installed { get; init; }
    public VpnActiveStatus Active { get; init; }
    public List<int> Ports { get; init; } = new();
    public long BytesReceived { get; init; }
    public long BytesSent { get; init; }
    public int Clients { get; init; }
    public int OnlineClients { get; init; }
}