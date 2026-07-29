public static class XrayConfigBuilder
{
    public static object BuildVlessInbound(ServerData serverData, ClientsData clientsData)
    {
        var security = VpnHelper.ParseXraySecurity(serverData.Xray.Vless.Security);

        var actualFlow = security == XraySecurity.NONE ? null : serverData.Xray.Vless.Flow;
        if (string.IsNullOrEmpty(actualFlow) || actualFlow == "none") actualFlow = null;

        var clients = new List<object>
        {
            new { id = serverData.Xray.Vless.DefaultUuid, flow = actualFlow }
        };

        var sIds = new List<string>();

        if (clientsData?.Clients != null)
        {
            var activeVlessClients = clientsData.Clients
                .OfType<VlessClient>()
                .Where(c => c.IsActive);

            foreach (var client in activeVlessClients)
            {
                clients.Add(new { id = client.Uuid, flow = actualFlow });

                if (!string.IsNullOrEmpty(client.ShortId))
                    sIds.Add(client.ShortId);
            }
        }

        if (sIds.Count == 0)
        {
            sIds.Add("");
        }

        object streamSettings = security switch
        {
            XraySecurity.REALITY => new
            {
                network = "tcp",
                security = "reality",
                realitySettings = new
                {
                    show = false,
                    dest = $"{serverData.Xray.Vless.Sni}:443",
                    xver = 0,
                    serverNames = new[]
                    {
                        serverData.Xray.Vless.Sni,
                        serverData.Xray.Vless.Sni.Replace("www.", "")
                    },
                    privateKey = serverData.Xray.Vless.Reality.PrivateKey,
                    shortIds = sIds.ToArray(),
                    fingerprint = serverData.Xray.Vless.Fingerprint
                }
            },
            XraySecurity.TLS => new
            {
                network = "tcp",
                security = "tls",
                tlsSettings = new
                {
                    serverName = serverData.Xray.Vless.Sni,
                    certificates = new[] { new { certificateFile = "/etc/xray/server.crt", keyFile = "/etc/xray/server.key" } }
                }
            },
            XraySecurity.NONE => new { network = "tcp", security = "none" },
            _ => throw new ArgumentException("Unsupported security type")
        };

        return new
        {
            tag = "vless-in",
            listen = "0.0.0.0",
            port = serverData.Xray.Vless.Port,
            protocol = "vless",
            settings = new { clients = clients.ToArray(), decryption = "none" },
            streamSettings = streamSettings,
            sniffing = new { enabled = true, destOverride = new[] { "http", "tls", "quic" }, routeOnly = true }
        };
    }

    public static object BuildSocksInbound(ServerData serverData, ClientsData clientsData)
    {
        var users = new List<object>();

        if (clientsData?.Clients != null)
        {
            var activeSocksClients = clientsData.Clients
                .OfType<SocksClient>()
                .Where(c => c.IsActive);

            foreach (var client in activeSocksClients)
            {
                users.Add(new
                {
                    user = client.Name,
                    pass = client.Password
                });
            }
        }

        return new
        {
            tag = "socks-in",
            listen = "0.0.0.0",
            port = serverData.Xray.Socks.Port,
            protocol = "socks",
            settings = new
            {
                auth = "password",
                accounts = users.ToArray(),
                udp = true
            }
        };
    }

    public static object BuildShadowsocksInbound(ServerData serverData, ClientsData clientsData)
    {
        var clients = new List<object>();

        if (clientsData?.Clients != null)
        {
            var activeSsClients = clientsData.Clients
                .OfType<ShadowsocksClient>()
                .Where(c => c.IsActive);

            foreach (var client in activeSsClients)
            {
                clients.Add(new
                {
                    method = client.Method,
                    password = client.Password,
                    email = client.Name
                });
            }
        }

        return new
        {
            tag = "shadowsocks-in",
            listen = "0.0.0.0",
            port = serverData.Xray.Shadowsocks.Port,
            protocol = "shadowsocks",
            settings = new
            {
                clients = clients.ToArray(),
                network = "tcp,udp"
            }
        };
    }
}