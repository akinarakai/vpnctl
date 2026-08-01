public class SystemStats
{
    public double CpuUsage { get; init; }
    public long TotalMemory { get; init; }
    public long UsageMemory { get; init; }
    public TimeSpan Uptime { get; init; }
    public double LoadAverage { get; init; }
}