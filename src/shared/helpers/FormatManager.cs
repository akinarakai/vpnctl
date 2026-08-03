public static class FormatManager
{
    public static string GetVpnNameFromType(VpnServiceType type)
    {
        return type switch
        {
            VpnServiceType.WIREGUARD => "WireGuard",
            VpnServiceType.AMNEZIAWG => "AmneziaWG",
            VpnServiceType.XRAY => "Xray",
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public static VpnServiceType GetVpnTypeFromShortName(string name)
    {
        return name switch
        {
            "wg" => VpnServiceType.WIREGUARD,
            "awg" => VpnServiceType.AMNEZIAWG,
            "xray" => VpnServiceType.XRAY,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public static ProtocolType GetProtocolFromShortName(string name)
    {
        return name switch
        {
            "wg" => ProtocolType.WIREGUARD,
            "awg" => ProtocolType.AMNEZIAWG,
            "vless" => ProtocolType.VLESS,
            "socks" => ProtocolType.SOCKS,
            "ss" => ProtocolType.SHADOWSOCKS,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public static string GetProtocolNameFromType(ProtocolType type)
    {
        return type switch
        {
            ProtocolType.WIREGUARD => "WireGuard",
            ProtocolType.AMNEZIAWG => "AmneziaWG",
            ProtocolType.VLESS => "VLESS",
            ProtocolType.SOCKS => "SOCKS",
            ProtocolType.SHADOWSOCKS => "Shadowsocks",
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public static (string down, string up) FormatTraffic(long bytesReceived, long bytesSent)
    {
        string FormatBytes(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int counter = 0;
            double number = bytes;

            while (Math.Round(number / 1024) >= 1 && counter < suffixes.Length - 1)
            {
                number /= 1024;
                counter++;
            }

            return counter == 0
                ? $"{bytes} B"
                : $"{number:F2} {suffixes[counter]}";
        }

        return (FormatBytes(bytesReceived), FormatBytes(bytesSent));
    }

    public static string GetRelativeTime(DateTime utcTime)
    {
        var now = DateTime.UtcNow;
        var diff = now - utcTime;

        if (diff.TotalSeconds < 2)
            return "just now";

        if (diff.TotalSeconds < 60)
            return $"{(int)diff.TotalSeconds} sec ago";

        if (diff.TotalMinutes < 60)
            return $"{(int)diff.TotalMinutes} min ago";

        if (diff.TotalHours < 24)
            return $"{(int)diff.TotalHours} hours ago";

        if (diff.TotalDays < 30)
            return $"{(int)diff.TotalDays} days ago";

        if (diff.TotalDays < 365)
            return $"{(int)(diff.TotalDays / 30)} months ago";

        return utcTime.ToString("dd.MM.yyyy");
    }

    public static string GetUptime(TimeSpan uptime)
    {
        if (uptime.TotalDays >= 1)
        {
            return $"{uptime.Days}d {uptime.Hours}h {uptime.Minutes}m";
        }

        if (uptime.TotalHours >= 1)
        {
            return $"{uptime.Hours}h {uptime.Minutes}m";
        }

        if (uptime.TotalMinutes >= 1)
        {
            return $"{uptime.Minutes}m";
        }

        return $"{uptime.Seconds}s";
    }
}