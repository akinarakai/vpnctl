public class SystemConfigurator : ISystemConfigurator
{
    public void CreateSysctlConfig()
    {
        var dataProvider = Kernel.Get<IDataProvider>();
        var config = dataProvider.GetServerState().SysctlConfig;

        var configStr = ConfigFormatBuilder.GetSysctlString(config);

        if (Kernel.Get<IFileManager>().TrySaveFile(PathRegistry.SysctlConf, configStr))
        {
            ExecuteSystem();

            Logger.Success("System config applied successfully.");
        }
    }

    public void DeleteSysctlConfig()
    {
        var cmd = Kernel.Get<ICommandRunner>();
        Kernel.Get<IFileManager>().Delete(PathRegistry.SysctlConf);

        cmd.Run("sysctl", "-w net.ipv4.ip_forward=0", true, false);
        cmd.Run("sysctl", "-w net.ipv6.conf.all.forwarding=0", true, false);

        ExecuteSystem();

        Logger.Success("System optimizations removed. Linux settings rolled back to defaults.");
    }

    private void ExecuteSystem()
    {
        var result = Kernel.Get<ICommandRunner>().Run("sysctl", "--system", true, false);
        if (!result.Success)
        {
            throw new Exception($"Failed execute: sysctl --system: {result.Text.Trim()}");
        }
    }

    private void ReloadDaemon()
    {
        var result = Kernel.Get<ICommandRunner>().Run("systemctl", "daemon-reload", true, false);
        if (!result.Success)
        {
            throw new Exception($"Failed execute: systemctl daemon-reload: {result.Text.Trim()}");
        }
    }
}