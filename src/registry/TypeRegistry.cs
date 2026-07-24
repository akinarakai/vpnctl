public class TypeRegistry
{
    private readonly Dictionary<Type, Func<object>> _factories = new();
    private readonly Dictionary<Type, object> _instances = new();

    public void Register<TInterface>(Func<TInterface> factory) where TInterface : class
    {
        _factories[typeof(TInterface)] = () => factory();
    }

    public bool IsCreated<T>() where T : class
    {
        return _instances.ContainsKey(typeof(T));
    }

    public T Get<T>() where T : class
    {
        var type = typeof(T);
        if (_instances.TryGetValue(type, out var instance)) return (T)instance;

        if (_factories.TryGetValue(type, out var factory))
        {
            var newInstance = (T)factory();
            _instances[type] = newInstance;
            return newInstance;
        }
        throw new InvalidOperationException($"Модуль {type.Name} не зарегистрирован в TypeRegistry.");
    }
}