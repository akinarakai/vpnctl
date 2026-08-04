public class ClientOnlineNetStats
{
    public string? Endpoint { get; init; }
    public long BytesRecived { get; init; } = 0;
    public long BytesSent { get; init; } = 0;
    public DateTime? LastConnectAt { get; init; } = null;
}