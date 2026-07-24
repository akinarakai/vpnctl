using System.Text.Json;

public class JsonDataProvider : IDataProvider
{
    private readonly string _serverPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "server.json");
    private readonly string _clientsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "clients.json");

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver()
    };

    private ServerData? _cachedServer = null;
    private ClientsData? _cachedClients = null;

    private string _initialServerHash = string.Empty;
    private string _initialClientsHash = string.Empty;


    public ServerData GetServerState()
    {
        if (_cachedServer != null) return _cachedServer;

        try
        {
            if (File.Exists(_serverPath))
            {
                var json = File.ReadAllText(_serverPath);
                _cachedServer = JsonSerializer.Deserialize<ServerData>(json, _jsonOptions) ?? new ServerData();
                _initialServerHash = HashHelper.CalculateHash(json);
            }
            else
            {
                _cachedServer = new ServerData();
                _initialServerHash = string.Empty;
            }

            return _cachedServer;
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to get server.json: {ex.Message}");
        }
    }

    public ClientsData GetClientsState()
    {
        if (_cachedClients != null) return _cachedClients;

        try
        {
            if (File.Exists(_clientsPath))
            {
                var json = File.ReadAllText(_clientsPath);
                _cachedClients = JsonSerializer.Deserialize<ClientsData>(json, _jsonOptions) ?? new ClientsData();
                _initialClientsHash = HashHelper.CalculateHash(json);
            }
            else
            {
                _cachedClients = new ClientsData();
                _initialClientsHash = string.Empty;
            }

            return _cachedClients;
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to get clients.json: {ex.Message}");
        }
    }

    public void TrySave()
    {
        try
        {
            if (_cachedServer != null)
            {
                var serverJson = JsonSerializer.Serialize(_cachedServer, _jsonOptions);
                var currentServerHash = HashHelper.CalculateHash(serverJson);

                if (currentServerHash != _initialServerHash)
                {
                    EnsureDirectoryExists(_serverPath);
                    File.WriteAllText(_serverPath, serverJson);
                    _initialServerHash = currentServerHash;
                }
            }

            if (_cachedClients != null)
            {
                var clientsJson = JsonSerializer.Serialize(_cachedClients, _jsonOptions);
                var currentClientsHash = HashHelper.CalculateHash(clientsJson);

                if (currentClientsHash != _initialClientsHash)
                {
                    EnsureDirectoryExists(_clientsPath);
                    File.WriteAllText(_clientsPath, clientsJson);
                    _initialClientsHash = currentClientsHash;
                }
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to save data state: {ex.Message}");
        }
    }

    public VpnClientBase? GetClient(string name)
    {
        var state = GetClientsState();
        return state.Clients.FirstOrDefault(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    public bool IsNameExist(string name)
    {
        var state = GetClientsState();
        return state.Clients.Any(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    private void EnsureDirectoryExists(string filePath)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (directory != null && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }
}