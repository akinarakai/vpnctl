public static class VpnManager
{
    private static readonly KeyRegistry<string> _registry = new(StringComparer.OrdinalIgnoreCase);
    private static readonly List<string> _registeredKeys = new();

    public static void Register<T>(Func<T> factory) where T : class, IVpnService
    {
        var key = GetNameFromType(typeof(T)).ToLower();

        _registeredKeys.Add(key);
        _registry.Register(key, factory);
    }

    public static T GetType<T>() where T : class, IVpnService
    {
        var key = GetNameFromType(typeof(T)).ToLower();
        return _registry.Get<T>(key);
    }

    public static IVpnService? Get(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        return _registry.TryGet<IVpnService>(name);
    }

    public static IReadOnlyList<IVpnService> GetAll()
    {
        var result = new List<IVpnService>();
        foreach (var key in _registeredKeys)
        {
            result.Add(_registry.Get<IVpnService>(key));
        }
        return result;
    }

    private static string GetNameFromType(Type type)
    {
        return type switch
        {
            _ when type == typeof(WireGuard) => "wg",
            _ when type == typeof(AmneziaWg) => "awg",
            _ when type == typeof(Xray) => "xray",

            _ => throw new ArgumentOutOfRangeException(nameof(type), $"Неизвестный тип провайдера: {type.Name}")
        };
    }
}