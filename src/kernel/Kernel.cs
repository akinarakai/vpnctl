public static class Kernel 
{
    private static readonly TypeRegistry _registry = new();

    public static ICommandRunner Cmd => _registry.Get<ICommandRunner>();
    public static IDataProvider Data => _registry.Get<IDataProvider>();
    public static INetworkManager Network => _registry.Get<INetworkManager>();
    public static IFirewallManager Firewall => _registry.Get<IFirewallManager>();
    public static ISystemConfigurator SysConfig => _registry.Get<ISystemConfigurator>();

    public static void Register<TInterface>(Func<TInterface> factory) where TInterface : class
    {
        _registry.Register(factory);
    }

    public static bool IsCreated<T>() where T : class
    {
        return _registry.IsCreated<T>();
    }
}