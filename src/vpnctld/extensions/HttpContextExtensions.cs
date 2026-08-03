public static class HttpContextExtensions
{
    private const string AuthTokenKey = "AuthToken";

    public static void SetAuthToken(this HttpContext context, AuthToken token)
    {
        context.Items[AuthTokenKey] = token;
    }

    public static AuthToken? GetAuthToken(this HttpContext context)
    {
        return context.Items[AuthTokenKey] as AuthToken;
    }
}