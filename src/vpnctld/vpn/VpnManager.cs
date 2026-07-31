public static class VpnManager
{
    private static readonly KeyRegistry<VpnServiceType> _registry = new();
    private static readonly List<VpnServiceType> _registeredKeys = new();

    public static WireGuard Wg => GetType<WireGuard>();
    public static AmneziaWg Awg => GetType<AmneziaWg>();
    public static Xray Xray => GetType<Xray>();

    public static void Register<T>(Func<T> factory) where T : class, IVpnService
    {
        var key = GetTypeFromType(typeof(T));

        _registeredKeys.Add(key);
        _registry.Register(key, factory);
    }

    public static T GetType<T>() where T : class, IVpnService
    {
        var key = GetTypeFromType(typeof(T));
        return _registry.Get<T>(key);
    }

    public static IVpnService? Get(VpnServiceType type)
    {
        return _registry.TryGet<IVpnService>(type);
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

    private static VpnServiceType GetTypeFromType(Type type)
    {
        return type switch
        {
            _ when type == typeof(WireGuard) => VpnServiceType.WIREGUARD,
            _ when type == typeof(AmneziaWg) => VpnServiceType.AMNEZIAWG,
            _ when type == typeof(Xray) => VpnServiceType.XRAY,

            _ => throw new ArgumentOutOfRangeException()
        };
    }
}