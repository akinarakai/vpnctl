public class ClientOnlineStats
{
    public string ClientId { get; init; } = string.Empty;
    public string? Endpoint { get; init; }
    public long BytesRecived { get; init; } = 0;
    public long BytesSent { get; init; } = 0;
    public DateTime? LastConnectAt { get; init; } = null;

    public override string ToString()
    {
        var lastConnectStr = LastConnectAt.HasValue
            ? LastConnectAt.Value.ToLocalTime().ToString("dd.MM.yyyy HH:mm:ss")
            : "Never";

        var (downStr, upStr) = FormatManager.FormatTraffic(BytesRecived, BytesSent);

        return $"ID: {ClientId} | EP: {Endpoint ?? "None"} | Last: {lastConnectStr} | Down: {downStr} | Up: {upStr}";
    }
}