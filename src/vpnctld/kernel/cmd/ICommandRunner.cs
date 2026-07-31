public class CmdResult
{
    public bool Success { get; init; }
    public string Text { get; init; } = string.Empty;
}

public interface ICommandRunner
{
    CmdResult Run(string command, string arguments, bool useSudo = false, bool showOutput = true);
}