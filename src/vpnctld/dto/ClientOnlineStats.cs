public class ClientOnlineStats
{
    public string ClientId { get; init; } = string.Empty;
    public string? Endpoint { get; init; }
    public long BytesRecived { get; init; } = 0;
    public long BytesSent { get; init; } = 0;
    public DateTime? LastConnectAt { get; init; } = null;

    public ClientOnlineNetStats ToNet()
    {
        return new ClientOnlineNetStats
        {   
            Endpoint = Endpoint,
            BytesRecived = BytesRecived,
            BytesSent = BytesSent,
            LastConnectAt = LastConnectAt
        };
    }
}