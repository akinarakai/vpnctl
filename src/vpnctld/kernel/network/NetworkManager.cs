using System.Net;

public class NetworkManager : INetworkManager
{
    private static string? _cachedIp = null;

    public string GetActiveNetInterface()
    {
        const string routeFilePath = "/proc/net/route";
        var interfaceName = "eth0";

        if (!File.Exists(routeFilePath))
        {
            Logger.Warn($"path \"{routeFilePath}\" not found, return fallback net interface {interfaceName}");
            return interfaceName;
        }

        var lines = File.ReadAllLines(routeFilePath);
        foreach (var line in lines.Skip(1))
        {
            var parts = line.Split(new[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2) continue;

            var name = parts[0];
            var dest = parts[1];

            if (name.StartsWith("awg") || name.StartsWith("wg"))
                continue;

            if (dest == "00000000")
            {
                interfaceName = name;
                break;
            }
        }

        Logger.Info($"Current network interface is \"{interfaceName}\"");
        return interfaceName;
    }

    public string GetIP()
    {
        if (_cachedIp != null) return _cachedIp;

        var data = Kernel.Data;
        var savedIp = data.GetServerState().ServerIpFallback?.Trim();

        if (!string.IsNullOrWhiteSpace(savedIp) && IPAddress.TryParse(savedIp, out _))
        {
            return _cachedIp = savedIp;
        }

        Logger.Info("Cached IP is missing or invalid. Resolving via external network service...");

        var cmd = Kernel.Cmd;

        var ipResult = cmd.Run("curl", "-s --connect-timeout 5 https://ifconfig.me", false, false);
        if (!ipResult.Success || string.IsNullOrWhiteSpace(ipResult.Text))
        {
            throw new Exception("IP resolution failed: External network service is unreachable, and no valid fallback IP is configured.");
        }

        var detectedIp = ipResult.Text.Trim();
        if (!IPAddress.TryParse(detectedIp, out _))
        {
            throw new Exception($"Network service returned an invalid IP address format: '{detectedIp}'");
        }

        data.GetServerState().ServerIpFallback = detectedIp;
        return _cachedIp = detectedIp;
    }

    public bool InterfaceExists(string name)
    {
        var result = Kernel.Cmd.Run(
            "ip",
            $"link show {name}",
            false,
            false
        );

        return result.Success;
    }

    public void CreateInterface(string name, string type)
    {
        ExecuteIpCommand($"link add dev {name} type {type}");
    }

    public void DeleteInterface(string name)
    {
        ExecuteIpCommand($"link delete {name}");
    }

    public void SetInterfaceUp(string name)
    {
        ExecuteIpCommand($"link set {name} up");
    }

    public void SetInterfaceDown(string name)
    {
        ExecuteIpCommand($"link set {name} down");
    }

    public void AddAddress(string interfaceName, string address)
    {
        ExecuteIpCommand($"addr add {address} dev {interfaceName}");
    }

    public void RemoveAddress(string interfaceName, string address)
    {
        ExecuteIpCommand($"addr del {address} dev {interfaceName}");
    }

    public void AddRoute(string destination, string interfaceName)
    {
        ExecuteIpCommand($"route add {destination} dev {interfaceName}");
    }

    public void RemoveRoute(string destination)
    {
        ExecuteIpCommand($"route del {destination}");
    }

    public void SetMtu(string interfaceName, int mtu)
    {
        ExecuteIpCommand($"link set {interfaceName} mtu {mtu}");
    }

    private void ExecuteIpCommand(string args)
    {
        var result = Kernel.Cmd.Run("ip", args, true, false);
        if (!result.Success)
        {
            throw new Exception($"Ip command failed: ip {args}. {result.Text.Trim()}");
        }

        Logger.Success($"Ip command success: ip {args}");
    }
}