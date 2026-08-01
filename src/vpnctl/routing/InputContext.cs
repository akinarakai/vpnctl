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
    public int Count => Args.Count;

    private readonly List<ParsedFlag> _flags = new();
    public IReadOnlyList<ParsedFlag> Flags => _flags;

    public InputContext(string[] args, IReadOnlyList<IInputFlag> supportedFlags)
    {
        Args = ParseContext(args, supportedFlags);
    }

    public bool HasFlag<T>() where T : IInputFlag
    {
        return _flags.Any(s => s.Value is T);
    }

    public bool HasArgs(params string[] args)
    {
        return args.Any(a => Args.Contains(a));
    }

    public bool TryGetFlag<T>(out ParsedFlag? flag) where T : IInputFlag
    {
        flag = _flags.FirstOrDefault(s => s.Value is T);
        return flag != null;
    }

    public string? GetFlagValue<TFlag>() where TFlag : IInputFlag
    {
        if (!TryGetFlag<TFlag>(out var flag))
            return null;

        if (flag?.Arguments == null || flag.Arguments.Count == 0)
            return null;

        return flag.Arguments[0];
    }

    private IReadOnlyList<string> ParseContext(string[] args, IReadOnlyList<IInputFlag> supportedFlags)
    {
        var cleanArgs = new List<string>();

        for (int i = 0; i < args.Length; i++)
        {
            var arg = args[i];

            if (arg.StartsWith("-") || arg.StartsWith("--"))
            {
                var cleanArg = arg.TrimStart('-');
                var matchedFlag = supportedFlags.FirstOrDefault(f =>
                    f.Name == cleanArg || (!string.IsNullOrEmpty(f.ShortName) && f.ShortName == cleanArg));

                if (matchedFlag != null)
                {
                    string[]? flagArgs = null;

                    if (matchedFlag.ArgumentCount > 0)
                    {
                        if (i + matchedFlag.ArgumentCount >= args.Length)
                        {
                            throw new ArgumentException($"Flag '{arg}' expects {matchedFlag.ArgumentCount} argument(s), but missing required values.");
                        }

                        flagArgs = new string[matchedFlag.ArgumentCount];

                        for (int j = 0; j < matchedFlag.ArgumentCount; j++)
                        {
                            var nextArg = args[i + j + 1];

                            if (nextArg.StartsWith("-") || nextArg.StartsWith("--"))
                            {
                                throw new ArgumentException($"Flag '{arg}' expects a value, but another flag was provided instead: '{nextArg}'.");
                            }

                            flagArgs[j] = nextArg;
                        }

                        i += matchedFlag.ArgumentCount;
                    }

                    _flags.Add(new ParsedFlag(matchedFlag, flagArgs));
                    continue;
                }
            }

            cleanArgs.Add(arg);
        }

        return cleanArgs;
    }
}