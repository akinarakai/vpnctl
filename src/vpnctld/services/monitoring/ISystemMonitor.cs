public interface ISystemMonitor
{
    SystemStats GetStats();
    TimeSpan GetUptime(ICommandRunner cmd);
}