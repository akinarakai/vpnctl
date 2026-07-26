using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Buffers.Text;

public static class FormatManager
{

    public static string GetWgServerConfString(ServerData serverData, ClientsData clientsData)
    {
        var sb = new StringBuilder();

        sb.AppendLine("[Interface]");
        sb.AppendLine($"Address = {serverData.Wg.BaseIp}");
        sb.AppendLine($"ListenPort = {serverData.Wg.Port}");
        sb.AppendLine($"PrivateKey = {serverData.Wg.PrivateKey}");
        sb.AppendLine();
        sb.AppendLine($"PostUp = iptables -I FORWARD -i %i -j ACCEPT; iptables -I FORWARD -o %i -j ACCEPT; iptables -t nat -I POSTROUTING -o {serverData.NetworkInterface} -j MASQUERADE");
        sb.AppendLine($"PostDown = iptables -D FORWARD -i %i -j ACCEPT; iptables -D FORWARD -o %i -j ACCEPT; iptables -t nat -D POSTROUTING -o {serverData.NetworkInterface} -j MASQUERADE");
        sb.AppendLine();

        if (clientsData?.Clients != null)
        {
            var activeWgClients = clientsData.Clients
                .OfType<WireGuardClient>()
                .Where(c => c.IsActive);

            foreach (var wg in activeWgClients)
            {
                sb.AppendLine("[Peer]");
                sb.AppendLine($"PublicKey = {wg.PublicKey}");
                sb.AppendLine($"AllowedIPs = {wg.AllowedIp}/32");
                sb.AppendLine();
            }
        }

        return sb.ToString().Trim();
    }

    public static string GetWgClientConfString(ServerData serverData, string serverIp, string clientIp, string clientPrivateKey, string serverPublicKey)
    {
        return $@"
[Interface]
PrivateKey = {clientPrivateKey}
Address = {clientIp}/32
DNS = 1.1.1.1
MTU = 1360

[Peer]
PublicKey = {serverPublicKey}
Endpoint = {serverIp}:{serverData.Wg.Port}
AllowedIPs = 0.0.0.0/0
PersistentKeepalive = 20
";
    }

    public static string BuildAwgServerConfig(ServerData serverData, ClientsData clientsData)
    {
        var awg = serverData.Awg;
        var sb = new StringBuilder();

        sb.AppendLine("[Interface]");
        sb.AppendLine($"PrivateKey = {awg.PrivateKey}");
        sb.AppendLine($"Address = {awg.BaseIp}");
        sb.AppendLine($"MTU = 1280");
        sb.AppendLine($"ListenPort = {awg.Port}");

        sb.AppendLine($"PostUp = iptables -I FORWARD -i %i -j ACCEPT; iptables -t nat -A POSTROUTING -o {serverData.NetworkInterface} -j MASQUERADE");
        sb.AppendLine($"PostDown = iptables -D FORWARD -i %i -j ACCEPT; iptables -t nat -D POSTROUTING -o {serverData.NetworkInterface} -j MASQUERADE");

        sb.AppendLine($"Jc = {awg.Jc}");
        sb.AppendLine($"Jmin = {awg.Jmin}");
        sb.AppendLine($"Jmax = {awg.Jmax}");

        sb.AppendLine($"S1 = {awg.S1}");
        sb.AppendLine($"S2 = {awg.S2}");
        sb.AppendLine($"S3 = {awg.S3}");
        sb.AppendLine($"S4 = {awg.S4}");

        sb.AppendLine($"H1 = {awg.H1}");
        sb.AppendLine($"H2 = {awg.H2}");
        sb.AppendLine($"H3 = {awg.H3}");
        sb.AppendLine($"H4 = {awg.H4}");

        sb.AppendLine($"I1 = {awg.I1}");
        sb.AppendLine();

        if (clientsData?.Clients != null)
        {
            var activeAwgClients = clientsData.Clients
                .OfType<AmneziaWgClient>()
                .Where(c => c.IsActive);

            foreach (var client in activeAwgClients)
            {
                sb.AppendLine("[Peer]");
                sb.AppendLine($"PublicKey = {client.PublicKey}");
                sb.AppendLine($"AllowedIPs = {client.AllowedIp}/32");
                sb.AppendLine();
            }
        }

        return sb.ToString();
    }

    public static string GetAWgClientConfig(ServerData serverData, string allowedIp, string privateKey, string serverIp)
    {
        var awg = serverData.Awg;
        var sb = new StringBuilder();

        sb.AppendLine("[Interface]");
        sb.AppendLine($"PrivateKey = {privateKey}");
        sb.AppendLine($"Address = {allowedIp}/32");
        sb.AppendLine("DNS = 1.1.1.1");
        sb.AppendLine("MTU = 1280");

        sb.AppendLine($"Jc = {awg.Jc}");
        sb.AppendLine($"Jmin = {awg.Jmin}");
        sb.AppendLine($"Jmax = {awg.Jmax}");

        sb.AppendLine($"S1 = {awg.S1}");
        sb.AppendLine($"S2 = {awg.S2}");
        sb.AppendLine($"S3 = {awg.S3}");
        sb.AppendLine($"S4 = {awg.S4}");

        sb.AppendLine($"H1 = {awg.H1}");
        sb.AppendLine($"H2 = {awg.H2}");
        sb.AppendLine($"H3 = {awg.H3}");
        sb.AppendLine($"H4 = {awg.H4}");

        sb.AppendLine($"I1 = {awg.I1}");
        sb.AppendLine();

        sb.AppendLine("[Peer]");
        sb.AppendLine($"PublicKey = {awg.PublicKey}");
        sb.AppendLine($"Endpoint = {serverIp}:{awg.Port}");
        sb.AppendLine("AllowedIPs = 0.0.0.0/0");
        sb.AppendLine("PersistentKeepalive = 33");

        return sb.ToString();
    }

    public static string GetXrayServerConfig(ServerData serverData, ClientsData clientsData)
    {
        var inboundsList = new List<object>
    {
        XrayConfigBuilder.BuildVlessInbound(serverData, clientsData),
        XrayConfigBuilder.BuildSocksInbound(serverData, clientsData),
        XrayConfigBuilder.BuildShadowsocksInbound(serverData, clientsData)
    };

        var xrayConfigObject = new
        {
            log = new
            {
                loglevel = serverData.Xray.LogLevel
            },
            inbounds = inboundsList.ToArray(),
            outbounds = new object[]
            {
            new { protocol = "freedom", tag = "direct" },
            new { protocol = "blackhole", tag = "block" }
            }
        };

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        return JsonSerializer.Serialize(xrayConfigObject, options);
    }

    public static string GetVlessClientUrl(ServerData serverData, string clientUuid, string clientName, string clientShortId, string serverIp)
    {
        const string encryption = "none";
        const string type = "tcp";

        var safeClientName = Uri.EscapeDataString(clientName.Trim());

        var queryParams = $"?encryption={encryption}" +
                          $"&type={type}" +
                          $"&security={serverData.Xray.Vless.Security.ToLower().Trim()}";

        var security = VpnHelper.ParseXraySecurity(serverData.Xray.Vless.Security);

        switch (security)
        {
            case XraySecurity.REALITY:
                var reality = serverData.Xray.Vless.Reality;

                var safePublicKey = reality.PublicKey
                    .Replace("+", "-")
                    .Replace("/", "_")
                    .TrimEnd('=');

                queryParams += $"&flow={serverData.Xray.Vless.Flow}" +
                               $"&sni={serverData.Xray.Vless.Sni}" +
                               $"&fp={serverData.Xray.Vless.Fingerprint}" +
                               $"&pbk={safePublicKey}";

                if (!string.IsNullOrWhiteSpace(clientShortId))
                {
                    queryParams += $"&sid={clientShortId.Trim()}";
                }
                break;

            case XraySecurity.TLS:
                queryParams += $"&sni={serverData.Xray.Vless.Sni}" +
                               $"&fp={serverData.Xray.Vless.Fingerprint}";

                if (!string.IsNullOrWhiteSpace(serverData.Xray.Vless.Flow))
                {
                    queryParams += $"&flow={serverData.Xray.Vless.Flow}";
                }
                break;

            case XraySecurity.NONE:
                break;
        }

        return $"vless://{clientUuid}@{serverIp}:{serverData.Xray.Vless.Port}{queryParams}#{safeClientName}";
    }

    public static string GetSocksClientUrl(ServerData serverData, string login, string password, string serverIp)
    {
        return $"socks://{login}:{password}@{serverIp}:{serverData.Xray.Socks.Port}#{login}";
    }

    public static string GetShadowsocksClientUrl(ServerData serverData, string login, string password, string method, string serverIp)
    {
        var rawUserInfo = $"{method}:{password}";

        var encodedUserInfo = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(rawUserInfo))
            .TrimEnd('=');

        return $"ss://{encodedUserInfo}@{serverIp}:{serverData.Xray.Shadowsocks.Port}#{Uri.EscapeDataString(login)}";
    }

    public static (string publicKey, string privateKey) ParseRealityKeyCommand(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
            throw new ArgumentException(nameof(command));

        string privateKey = "";
        string publicKey = "";

        foreach (var line in command.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = line.Split(':', 2);
            if (parts.Length != 2) continue;

            var name = parts[0].Trim().ToLowerInvariant();
            var value = parts[1].Trim();

            if (name == "privatekey" || name == "private key")
            {
                privateKey = value;
            }
            else if (name == "publickey"
                  || name == "public key"
                  || name == "password (publickey)"
                  || name == "password (public key)")
            {
                publicKey = value;
            }
        }

        return (publicKey, privateKey);
    }

    public static List<ClientOnlineStats> GetAwgOrWgOnlineStats(string dump)
    {
        var result = new List<ClientOnlineStats>();
        if (string.IsNullOrEmpty(dump)) return result;

        var lines = dump.Trim().Split('\n', StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            var parts = line.Split('\t');

            if (parts.Length < 7) continue;
            if (parts.Length > 10) continue;

            // parts[0] - interaface name (awg0, wg0)
            // parts[1] - public key
            // parts[2] - preshared-key
            // parts[3] - endpoint
            // parts[4] - allowed-ips
            // parts[5] - last handshake (unix timestamp)
            // parts[6] - bytes received
            // parts[7] - bytes transmitted

            var publicKey = parts[0].Trim();
            var endPoint = parts[2].Trim();

            if (endPoint == "none" || endPoint == "(none)")
            {
                endPoint = null;
            }

            DateTime? lastConnectAt = null;
            if (long.TryParse(parts[4].Trim(), out long unixTime) && unixTime > 0)
            {
                lastConnectAt = DateTimeOffset.FromUnixTimeSeconds(unixTime).UtcDateTime;
            }

            long.TryParse(parts[5].Trim(), out long rxBytes);
            long.TryParse(parts[6].Trim(), out long txBytes);

            var client = new ClientOnlineStats
            {
                ClientId = publicKey,
                Endpoint = endPoint,
                LastConnectAt = lastConnectAt,
                BytesRecived = txBytes,
                BytesSent = rxBytes
            };

            //System.Console.WriteLine(client.ToString());

            result.Add(client);
        }

        return result;
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
}