public static class ApiClient
{
    private static Client? _current;

    public static Client Current
    {
        get
        {
            if (_current == null)
            {
                var current = Kernel.Get<IServersProfileProvider>().GetCurrent();
                if (current == null)
                    throw new Exception($"Current server profile not found!");

                _current = Create(current);
            }

            return _current;
        }
    }

    public static Client Create(ServerProfile profile)
    {
        var http = new HttpClient
        {
            BaseAddress = new Uri(CreateUrl(profile.Address, profile.Port)),
            Timeout = TimeSpan.FromSeconds(10)
        };

        if (!string.IsNullOrEmpty(profile.Token))
        {
            http.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", profile.Token);
        }

        return new Client(http);
    }

    public static void Reset()
    {
        _current = null;
    }

    private static string CreateUrl(string address, int port)
    {
        var builder = new UriBuilder
        {
            Scheme = "http",
            Host = address,
            Port = port
        };

        return builder.Uri.ToString();
    }
}