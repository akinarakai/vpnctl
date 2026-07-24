public class UfwFirewallManager : IFirewallManager
{
    public bool OpenUdp(int port)
    {
        return ExecuteCommand($"allow {port}/udp");
    }

    public bool CloseUdp(int port)
    {
        return ExecuteCommand($"delete allow {port}/udp");
    }

    public bool OpenTcp(int port)
    {
        return ExecuteCommand($"allow {port}/tcp");
    }

    public bool CloseTcp(int port)
    {
        return ExecuteCommand($"delete allow {port}/tcp");
    }

    private bool ExecuteCommand(string args)
    {
        var result = Kernel.Cmd.Run("ufw", args, true, false);

        if (!result.Success)
            throw new Exception($"Firewall command failed: ufw {args} {result.Text.Trim()}");

        Logger.Success($"Firewall command success: {args}");
        return true;
    }
}