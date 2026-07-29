public class KeyRegistry<TKey> where TKey : notnull
{
    private readonly Dictionary<TKey, Func<object>> _factories;
    private readonly Dictionary<TKey, object> _instances;

    public KeyRegistry(IEqualityComparer<TKey>? comparer = null)
    {
        _factories = new Dictionary<TKey, Func<object>>(comparer);
        _instances = new Dictionary<TKey, object>(comparer);
    }

    public void Register<T>(TKey key, Func<T> factory) where T : class
    {
        _factories[key] = () => factory();
    }

    public bool IsCreated(TKey key)
    {
        return _instances.ContainsKey(key);
    }

    public T Get<T>(TKey key) where T : class
    {
        if (_instances.TryGetValue(key, out var instance)) return (T)instance;

        if (_factories.TryGetValue(key, out var factory))
        {
            var newInstance = (T)factory();
            _instances[key] = newInstance;
            return newInstance;
        }
        throw new InvalidOperationException($"Объект с ключом '{key}' не зарегистрирован.");
    }

    public T? TryGet<T>(TKey key) where T : class
    {
        if (_instances.TryGetValue(key, out var instance)) 
            return (T)instance;
        if (_factories.TryGetValue(key, out var factory))
        {
            var newInstance = (T)factory();
            _instances[key] = newInstance;
            return newInstance;
        }

        return null;
    }
}