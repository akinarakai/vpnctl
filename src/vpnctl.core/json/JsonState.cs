using System.Text.Json;

public class JsonState<T> where T : new()
{
    private readonly string _path;
    private readonly IFileManager _fileManager;
    private readonly JsonSerializerOptions _options;

    private T? _data;

    public T Value => Get();

    public JsonState(string path, IFileManager fileManager, JsonSerializerOptions options)
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
                return _data;
            }

            _data = JsonSerializer.Deserialize<T>(json, _options) ?? new T();
        }
        else
        {
            _data = new T();
        }

        return _data;
    }

    public bool Save()
    {
        if (_data == null)
            return false;

        var json = JsonSerializer.Serialize(_data, _options);

        return _fileManager.TrySave(_path, json);
    }

    public void Delete()
    {
        _fileManager.Delete(_path);
    }
}