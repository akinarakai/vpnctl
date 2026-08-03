

public static class AuthTokenExtensions
{
    public static AuthTokenNetData ToNet(this AuthToken token)
    {
        return new  AuthTokenNetData
        {
            Name = token.Name,
            Level = token.Level,
            CreatedAt = token.CreatedAt,
            LastUsedAt = token.LastUsedAt,  
        };
    }
}