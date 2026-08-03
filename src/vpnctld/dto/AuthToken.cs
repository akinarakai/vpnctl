public class AuthToken
{
    public string Name { get; init; } = string.Empty;
    public string TokenHash { get; init; } = string.Empty;
    public AccessLevel Level { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? LastUsedAt { get; set; }
}