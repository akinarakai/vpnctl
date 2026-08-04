using System.Text.Json;

public class ServersProfileProvider : IServersProfileProvider
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true
    };

    private JsonStorage<ProfileStorage>? _storage;

    public ProfileStorage GetStorage()
    {
        if (_storage == null)
        {
            var directory = string.Empty;

            if (OperatingSystem.IsLinux())
            {
                directory = "/etc/vpnctl";
            }
            else if (OperatingSystem.IsWindows())
            {
                directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "vpnctl");
            }
            else
            {
                throw new PlatformNotSupportedException();
            }

            var path = Path.Combine(directory, "servers.json");
            _storage = new(path, Kernel.Get<IFileManager>(), _jsonOptions);
        }

        return _storage.Get();
    }

    public void TrySave()
    {
        _storage?.Save();
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