using System.Text;
using System.Text.Json;

public class JsonStorage<T> : IStorage where T : new()
{
    private readonly string _path;
    private readonly IFileManager _fileManager;
    private readonly JsonSerializerOptions _options;

    private T? _data;
    private string _hash = string.Empty;

    public T Value => Get();

    public JsonStorage(string path, IFileManager fileManager, JsonSerializerOptions options)
    {
        _path = path;
        _fileManager = fileManager;
        _options = options;
    }

    public T Get()
    {
        if (_data != null)
            return _data;

        if (_fileManager.Exists(_path))
        {
            if (!_fileManager.TryRead(_path, out var json))
            {
                Logger.Warn($"Failed to read '{_path}', using default value");

                _data = new T();
                _hash = string.Empty;
                return _data;
            }

            _data = JsonSerializer.Deserialize<T>(json, _options) ?? new T();
            _hash = _fileManager.CalculateHash(json);
        }
        else
        {
            _data = new T();
            _hash = string.Empty;
        }

        return _data;
    }

    public bool Save()
    {
        if (_data == null)
            return false;

        var json = JsonSerializer.Serialize(_data, _options);
        var hash = _fileManager.CalculateHash(json);

        if (hash == _hash)
            return false;

        if (!_fileManager.TrySave(_path, json))
            return false;

        _hash = hash;
        return true;
    }

    public void Delete()
    {
        _fileManager.Delete(_path);

        _data = new T();
        _hash = string.Empty;
    }
}