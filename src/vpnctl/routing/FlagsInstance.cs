public static class FlagsInstance
{
    private static List<IInputFlag>? _flagsCache;

    public static IReadOnlyList<IInputFlag> GetAll()
    {
        if (_flagsCache != null) return _flagsCache;

        _flagsCache = new List<IInputFlag>();

        var assembly = typeof(IInputFlag).Assembly;

        var flagTypes = assembly.GetTypes()
            .Where(t => typeof(IInputFlag).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

        foreach (var type in flagTypes)
        {
            var flagInstance = (IInputFlag)Activator.CreateInstance(type)!;
            _flagsCache.Add(flagInstance);
        }

        return _flagsCache;
    }
}
