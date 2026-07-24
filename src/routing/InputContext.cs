public class ParsedFlag
{
    public IInputFlag Value { get; }

    public string Name => Value.Name;
    public IReadOnlyList<string>? Arguments { get; }

    public ParsedFlag(IInputFlag flag, string[]? arguments)
    {
        Value = flag;
        Arguments = arguments;
    }
}

public class InputContext
{
    public IReadOnlyList<string> Args { get; }
    public int Count { get; }

    private readonly List<ParsedFlag> _flags = new();
    public IReadOnlyList<ParsedFlag> Flags => _flags;

    public InputContext(string[] args, IReadOnlyList<IInputFlag> supportedFlags)
    {
        Args = args.ToList();
        Count = Args.Count;

        ParseFlags(Args, supportedFlags);
    }

    public bool HasFlag<T>() where T : IInputFlag
    {
        return _flags.Any(s => s.Value is T);
    }

    public bool TryGetFlag<T>(out ParsedFlag? flag) where T : IInputFlag
    {
        flag = _flags.FirstOrDefault(s => s.Value is T);
        return flag != null;
    }

    private void ParseFlags(IReadOnlyList<string> args, IReadOnlyList<IInputFlag> supportedFlags)
    {
        for (int i = 0; i < args.Count; i++)
        {
            var arg = args[i];
            if (arg.StartsWith("-") || arg.StartsWith("--"))
            {
                var cleanArg = arg.TrimStart('-');
                foreach (var flag in supportedFlags)
                {
                    if (flag.Name == cleanArg || (!string.IsNullOrEmpty(flag.ShortName) && flag.ShortName == cleanArg))
                    {
                        string[]? flagArgs = null;
                        if (flag.ArgumentCount > 0)
                        {
                            if (i + flag.ArgumentCount >= args.Count)
                            {
                                throw new ArgumentException($"Flag '--{cleanArg}' expects {flag.ArgumentCount} argument(s), but missing required values.");
                            }

                            flagArgs = new string[flag.ArgumentCount];

                            for (int j = 0; j < flag.ArgumentCount; j++)
                            {
                                var nextArg = args[i + j + 1];

                                if (nextArg.StartsWith("--"))
                                {
                                    throw new ArgumentException($"Flag '--{cleanArg}' expects a value, but another flag was provided instead: '{nextArg}'.");
                                }

                                flagArgs[j] = nextArg;
                            }

                            i = i + flag.ArgumentCount;
                        }

                        _flags.Add(new(flag, flagArgs));
                        break;
                    }
                }
            }
        }
    }
}