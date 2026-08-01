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
        var cmd = Kernel.Get<ICommandRunner>();

        var statusResult = cmd.Run("ufw", "status", true, false);
        if (!statusResult.Success)
        {
            Logger.Warn($"UFW firewall is not available or not installed. Skipping command: ufw {args}");
            return true;
        }

        var result = cmd.Run("ufw", args, true, false);
        if (!result.Success)
        {
            throw new Exception($"Firewall command failed: ufw {args}. {result.Text.Trim()}");
        }

        Logger.Success($"Firewall command success: ufw {args}");
        return true;
    }
}