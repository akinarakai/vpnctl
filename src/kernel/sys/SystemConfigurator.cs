using System.Text;

public class SystemConfigurator : ISystemConfigurator
{
    private readonly string _path = "/etc/sysctl.d/99-vpnctl.conf";

    public void ApplySystemOptimizations()
    {
        var dataProvider = Kernel.Data;
        var config = dataProvider.GetServerState().SysctlConfig;

        var sb = new StringBuilder();

        sb.AppendLine("# VPN-CTL Automatic Network Configurations");
        sb.AppendLine($"net.ipv4.ip_forward={(config.IpV4Forwarding ? 1 : 0)}");
        sb.AppendLine($"net.ipv6.conf.all.forwarding={(config.IpV6Forwarding ? 1 : 0)}");
        sb.AppendLine($"net.core.somaxconn={config.MaxConnectionsBacklog}");
        sb.AppendLine($"net.core.netdev_max_backlog={config.MaxConnectionsBacklog}");
        sb.AppendLine("net.core.default_qdisc=fq");
        sb.AppendLine($"net.ipv4.tcp_congestion_control={config.CongestionControl}");
        sb.AppendLine($"net.ipv4.tcp_syncookies={(config.DisableIcmpEchoIgnoreAll ? 1 : 0)}");

        if (Kernel.File.TrySaveFile(_path, sb.ToString()))
        {
            var result = Kernel.Cmd.Run("sysctl", "--system", true, false);
            if (!result.Success)
            {
                throw new Exception($"Failed applying sys config: {result.Text.Trim()}");
            }

            Logger.Success("System config applied successfully.");
        }
    }

    public void DeleteSystemConfig()
    {
        Kernel.File.Delete(_path);

        Kernel.Cmd.Run("sysctl", "-w net.ipv4.ip_forward=0", true, false);
        Kernel.Cmd.Run("sysctl", "-w net.ipv6.conf.all.forwarding=0", true, false);

        var result = Kernel.Cmd.Run("sysctl", "--system", true, false);
        if (!result.Success)
        {
            throw new Exception($"Failed to reload sysctl after rollback: {result.Text.Trim()}");
        }

        Logger.Success("System optimizations removed. Linux settings rolled back to defaults.");
    }
}