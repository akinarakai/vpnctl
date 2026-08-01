public static class Kernel
{
    private static readonly TypeRegistry _registry = new();

    public static void Register<TInterface>(Func<TInterface> factory) where TInterface : class
    {
        _registry.Register(factory);
    }

    public static bool IsCreated<T>() where T : class
    {
        return _registry.IsCreated<T>();
    }

    public static T Get<T>() where T : class
    {
        return _registry.Get<T>();
    }

    public static void Clear()
    {
        //_registry.Clear();
    }
}