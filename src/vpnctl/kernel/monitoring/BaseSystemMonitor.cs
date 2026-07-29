using System.Globalization;

public class BaseSystemMonitor : ISystemMonitor
{
    public SystemStats GetStats()
    {
        var cmd = Kernel.Cmd;
        var memory = GetMemory(cmd);

        return new SystemStats
        {
            TotalMemory = memory.total,
            UsageMemory = memory.used,
            Uptime = GetUptime(cmd),
            LoadAverage = GetLoad(cmd),
            CpuUsage = GetCpuUsage(cmd)
        };
    }

    private (long total, long used) GetMemory(ICommandRunner cmd)
    {
        var result = cmd.Run("free", "-m", true, false);
        if (!result.Success)
            throw new Exception($"Failed to get memory info: {result.Text}");

        var lines = result.Text.Split('\n');
        foreach (var line in lines.Skip(1))
        {
            var words = line.Split(' ', '\t', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length < 3) continue;

            if (long.TryParse(words[1], out var total) && long.TryParse(words[2], out var used))
            {
                return (total, used);
            }
        }

        return (0, 0);
    }

    private TimeSpan GetUptime(ICommandRunner cmd)
    {
        var result = cmd.Run("uptime", "-s", true, false);
        if (!result.Success)
            throw new Exception($"Failed to get uptime: {result.Text}");

        var startTime = DateTime.Parse(result.Text.Trim());

        return DateTime.Now - startTime;
    }

    private double GetLoad(ICommandRunner cmd)
    {
        var result = cmd.Run("cat", "/proc/loadavg", true, false);

        if (!result.Success)
            return 0;

        var load = result.Text
            .Split(' ')[0];

        return double.Parse(
            load,
            CultureInfo.InvariantCulture
        );
    }

    private double GetCpuUsage(ICommandRunner cmd)
    {
        var result = cmd.Run("top", "-b -n 1", true, false);
        if (!result.Success) return 0;

        var lines = result.Text.Split('\n');
        foreach (var line in lines.Skip(2))
        {
            var words = line.Split(' ', '\t', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length < 5) continue;

            var value = words[7];

            if (double.TryParse(value.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var idle))
            {
                var usage = 100 - Math.Round(idle);
                return Math.Clamp(usage, 0, 100);
            }
        }

        return 0;
    }
}