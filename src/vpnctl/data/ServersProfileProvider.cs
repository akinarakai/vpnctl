using System.Text.Json;

public class ServersProfileProvider : IServersProfileProvider
{
    private readonly string _path = Path.Combine(PathRegistry.VpnctlDir, "servers.json");

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true
    };

    private ProfileStorage? _cached;

    private string _initialHash = string.Empty;

    public ProfileStorage GetStorage()
    {
        if (_cached != null)
            return _cached;

        try
        {
            var file = Kernel.Get<IFileManager>();

            if (file.Exists(_path))
            {
                file.TryRead(_path,  out var json);

                _cached = JsonSerializer.Deserialize<ProfileStorage>(json, _jsonOptions) ?? new ProfileStorage();
                _initialHash = file.CalculateHash(json);
            }
            else
            {
                _cached = new ProfileStorage();
                _initialHash = string.Empty;
            }

            return _cached;
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to get servers.json: {ex.Message}");
        }
    }

    public void TrySave()
    {
        try
        {
            if (_cached == null)
                return;

            var file = Kernel.Get<IFileManager>();

            var json = JsonSerializer.Serialize(_cached, _jsonOptions);
            var currentHash = file.CalculateHash(json);
            if (currentHash == _initialHash) return;

            file.TrySave(_path, json);

            _initialHash = currentHash;
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to save servers.json: {ex.Message}");
        }
    }

    public void Add(ServerProfile profile)
    {
        var storage = GetStorage();

        var exist = Get(profile.Name, storage.Profiles) != null;
        if (exist)
        {
            throw new Exception($"Server '{profile.Name}' already exists");
        }

        if (storage.Profiles.Count == 0)
        {
            storage.CurrentProfile = profile.Name;
        }

        storage.Profiles.Add(profile);
    }

    public void Remove(string name)
    {
        var storage = GetStorage();

        var profile = Get(name, storage.Profiles);
        if (profile == null)
        {
            throw new Exception($"Server profile '{name}' not found");
        }

        if (profile.Name == storage.CurrentProfile)
        {
            storage.CurrentProfile = null;
        }

        storage.Profiles.Remove(profile);
    }

    public void SetCurrent(string name)
    {
        var storage = GetStorage();

        if (Get(name, storage.Profiles) == null)
        {
            throw new Exception($"Server '{name}' not found");
        }

        storage.CurrentProfile = name;
    }

    public ServerProfile? GetCurrent()
    {
        var storage = GetStorage();
        if (storage.CurrentProfile == null)
            return null;

        var current = Get(storage.CurrentProfile, storage.Profiles);
        if (current == null)
            throw new Exception($"Current server profile not found!");

        return current;
    }

    public ServerProfile? Get(string name, List<ServerProfile>? profiles = null)
    {
        profiles ??= GetStorage().Profiles;

        return profiles.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }
}