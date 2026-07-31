public class SystemConfigurator : ISystemConfigurator
{
    public void CreateSysctlConfig()
    {
        var dataProvider = Kernel.Data;
        var config = dataProvider.GetServerState().SysctlConfig;

        var configStr = ConfigFormatBuilder.GetSysctlString(config);

        if (Kernel.File.TrySaveFile(PathRegistry.SysctlConf, configStr))
        {
            ExecuteSystem();

            Logger.Success("System config applied successfully.");
        }
    }

    public void DeleteSysctlConfig()
    {
        Kernel.File.Delete(PathRegistry.SysctlConf);

        Kernel.Cmd.Run("sysctl", "-w net.ipv4.ip_forward=0", true, false);
        Kernel.Cmd.Run("sysctl", "-w net.ipv6.conf.all.forwarding=0", true, false);

        ExecuteSystem();

        Logger.Success("System optimizations removed. Linux settings rolled back to defaults.");
    }

    private void ExecuteSystem()
    {
        var result = Kernel.Cmd.Run("sysctl", "--system", true, false);
        if (!result.Success)
        {
            throw new Exception($"Failed execute: sysctl --system: {result.Text.Trim()}");
        }
    }

    private void ReloadDaemon()
    {
        var result = Kernel.Cmd.Run("systemctl", "daemon-reload", true, false);
        if (!result.Success)
        {
            throw new Exception($"Failed execute: systemctl daemon-reload: {result.Text.Trim()}");
        }
    }
}