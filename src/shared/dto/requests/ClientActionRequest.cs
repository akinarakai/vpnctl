public class ClientActionRequest
{
    public ProtocolType? Protocol { get; init; }
    public ClientNetActionType Action { get; init; }

    public string? Name { get; init; }
    public bool NeedShortId { get; init; }
    public string? Password { get; init; }
}