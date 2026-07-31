using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

public static class ConfigFormatBuilder 
{
    private static readonly XrayConfigBuilder _xray = new();

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
MTU = {serverData.Wg.Mtu}

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
        sb.AppendLine($"MTU = {awg.Mtu}");
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
        sb.AppendLine($"MTU = {awg.Mtu}");

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
        _xray.BuildVlessInbound(serverData, clientsData),
        _xray.BuildSocksInbound(serverData, clientsData),
        _xray.BuildShadowsocksInbound(serverData, clientsData)
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

    public static string GetSysctlString(SysctlConfig config)
    {
        var sb = new StringBuilder();

        sb.AppendLine("# VPN-CTL Automatic Network Configurations");
        sb.AppendLine($"net.ipv4.ip_forward={(config.IpV4Forwarding ? 1 : 0)}");
        sb.AppendLine($"net.ipv6.conf.all.forwarding={(config.IpV6Forwarding ? 1 : 0)}");
        sb.AppendLine($"net.core.somaxconn={config.MaxConnectionsBacklog}");
        sb.AppendLine($"net.core.netdev_max_backlog={config.MaxConnectionsBacklog}");
        sb.AppendLine("net.core.default_qdisc=fq");
        sb.AppendLine($"net.ipv4.tcp_congestion_control={config.CongestionControl}");
        sb.AppendLine($"net.ipv4.tcp_syncookies={(config.DisableIcmpEchoIgnoreAll ? 1 : 0)}");

        return sb.ToString().Trim();
    }

    public static string GetWgServiceString(string wgPath, string wgQuickPath, string confPath)
    {
        /*
        [Unit]
        Description=WireGuard via wg-quick(8) for %I
        After=network-online.target nss-lookup.target
        Wants=network-online.target nss-lookup.target
        PartOf=wg-quick.target
        Documentation=man:wg-quick(8)
        Documentation=man:wg(8)
        Documentation=https://www.wireguard.com/
        Documentation=https://www.wireguard.com/quickstart/
        Documentation=https://git.zx2c4.com/wireguard-tools/about/src/man/wg-quick.8
        Documentation=https://git.zx2c4.com/wireguard-tools/about/src/man/wg.8

        [Service]
        Type=oneshot
        RemainAfterExit=yes
        ExecStart=/usr/bin/wg-quick up %i
        ExecStop=/usr/bin/wg-quick down %i
        ExecReload=/bin/bash -c 'exec /usr/bin/wg syncconf %i <(exec /usr/bin/wg-quick strip %i)'
        Environment=WG_ENDPOINT_RESOLUTION_RETRIES=infinity

        [Install]
        WantedBy=multi-user.target
        */

        var sb = new StringBuilder();

        sb.AppendLine("[Unit]");
        sb.AppendLine("Description=WireGuard via wg-quick(8) for %I");
        sb.AppendLine("After=network-online.target nss-lookup.target");
        sb.AppendLine("Wants=network-online.target nss-lookup.target");
        sb.AppendLine("PartOf=vpnctl.target");
        sb.AppendLine();
        sb.AppendLine("[Service]");
        sb.AppendLine("Type=oneshot");
        sb.AppendLine("RemainAfterExit=yes");
        sb.AppendLine($"ExecStart={wgQuickPath} up {confPath}/%I.conf");
        sb.AppendLine($"ExecStop={wgQuickPath} down {confPath}%I.conf");
        sb.AppendLine($"ExecReload=/bin/bash -c 'exec {wgPath} syncconf %I <(exec {wgQuickPath} strip {confPath}/%I.conf)'");
        sb.AppendLine("Environment=WG_ENDPOINT_RESOLUTION_RETRIES=infinity");
        sb.AppendLine();
        sb.AppendLine("[Install]");
        sb.AppendLine("WantedBy=multi-user.target");

        return sb.ToString().Trim();
    }

    public static string GetTargetString()
    {
        /*
        [Install]
        WantedBy=multi-user.target
        */

        var sb = new StringBuilder();

        sb.AppendLine("[Unit]");
        sb.AppendLine("Description=VPNctl Active Tunnels Group");
        sb.AppendLine();

        sb.AppendLine("[Install]");
        sb.AppendLine("WantedBy=multi-user.target");

        return sb.ToString().Trim();
    }
}