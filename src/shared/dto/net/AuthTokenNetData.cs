public class AuthTokenNetData
{
    public string Name { get; init; } = string.Empty;
    public AccessLevel Level { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? LastUsedAt { get; init; }
}