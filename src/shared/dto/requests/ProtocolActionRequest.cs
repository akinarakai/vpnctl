public class ProtocolActionRequest
{
    public ProtocolType Type { get; init; }
    public ProtocolNetActionType Action { get; init; }
    public string? Value { get; init; }
}