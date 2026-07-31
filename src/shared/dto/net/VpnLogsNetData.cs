public class VpnLogsNetData
{
    public VpnServiceType Type { get; init; }
    public List<string> LogsLines { get; init; } = new();
}