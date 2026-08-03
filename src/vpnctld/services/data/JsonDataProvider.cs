using System.Security.Cryptography;
using System.Text.Json;

public class JsonDataProvider : IDataProvider
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver()
    };

    private JsonState<ServerData>? _server;
    private JsonState<ClientsData>? _clients;
    private JsonState<List<AuthToken>>? _tokens;

    public ServerData GetServerState()
    {
        if (_server == null)
        {
            var path = Path.Combine(PathRegistry.VpnctlDir, "server.json");
            _server = new(path, Kernel.Get<IFileManager>(), _jsonOptions);
        }

        return _server.Get();
    }

    public ClientsData GetClientsState()
    {
        if (_clients == null)
        {
            var path = Path.Combine(PathRegistry.VpnctlDir, "clients.json");
            _clients = new(path, Kernel.Get<IFileManager>(), _jsonOptions);
        }

        return _clients.Get();
    }

    public List<AuthToken> GetTokens()
    {
        if (_tokens == null)
        {
            var path = Path.Combine(PathRegistry.VpnctlDir, "tokens.json");
            _tokens = new(path, Kernel.Get<IFileManager>(), _jsonOptions);
        }

        var tokens = _tokens.Get();

        if (!tokens.Any() || !tokens.Any(x => x.Level == AccessLevel.ADMIN))
        {
            var secret = GetSecret();

            var token = new AuthToken
            {
                Name = $"auto-generated-admin",
                TokenHash = TokenHasher.Hash(secret),
                Level = AccessLevel.ADMIN
            };

            tokens.Add(token);

            Logger.Info($"Created admin token: {secret}");

            _tokens?.Save();
        }

        return tokens;
    }

    public void TrySave()
    {
        _server?.Save();
        _clients?.Save();
        _tokens?.Save();
    }

    public void DeleteFiles()
    {
        _server?.Delete();
        _clients?.Delete();
        _tokens?.Delete();
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

    public AuthToken? GetToken(string token)
    {
        return GetTokens().FirstOrDefault(c => TokenHasher.Verify(token, c.TokenHash));
    }

    public void AddToken(string name, AccessLevel level, out string secret)
    {
        var tokens = GetTokens();

        secret = GetSecret();

        var token = new AuthToken
        {
            Name = name,
            TokenHash = TokenHasher.Hash(secret),
            Level = level
        };

        tokens.Add(token);

        Logger.Info($"Created new token {name}, level: {level.ToString()}");

        _tokens?.Save();
    }

    public void RemoveToken(string name)
    {
        var tokens = GetTokens();

        var token = tokens.FirstOrDefault(t => t.Name == name);
        if (token == null)
            throw new Exception($"Token with name '{name}' not exist");

        tokens.Remove(token);

        Logger.Info($"Token with name '{name}' was removed.");

        _tokens?.Save();
    }

    private string GetSecret()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }
}