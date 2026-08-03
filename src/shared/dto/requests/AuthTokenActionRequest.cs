public class AuthTokenActionRequest
{
    public string Name { get; init; } = string.Empty;
    public AuthTokenNetAction Action { get; init; }
    public AccessLevel? AccessLevel { get; init; }
}