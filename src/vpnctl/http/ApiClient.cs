public static class ApiClient
{
    private static Client? _client = null;

    public static Client Get()
    {
        if (_client == null)
        {
            var http = new HttpClient();
            http.BaseAddress = new Uri("http://127.0.0.1:5180");
            http.Timeout = TimeSpan.FromSeconds(10);

            _client = new(http);
        }
            
        return _client;
    }
}