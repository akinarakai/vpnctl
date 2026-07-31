public static class ClientHelper
{
    public static ProtocolType GetProtocolFromClientType(VpnClientBase client)
    {
        return client switch
        {
            WireGuardClient => ProtocolType.WIREGUARD,
            AmneziaWgClient => ProtocolType.AMNEZIAWG,
            VlessClient => ProtocolType.VLESS,
            SocksClient => ProtocolType.SOCKS,
            ShadowsocksClient => ProtocolType.SHADOWSOCKS,
            _ => throw new NotSupportedException(
                $"Unknown client type: {client.GetType().Name}")
        };
    }

    public static Dictionary<string, ClientOnlineStats> GetOnlineStats()
    {
        var result = new Dictionary<string, ClientOnlineStats>();

        foreach (var vpn in VpnManager.GetAll())
        {
            foreach (var stat in vpn.GetOnlineStats())
            {
                result[stat.ClientId] = stat;
            }
        }

        return result;
    }

    public static ClientOnlineStats? GetOnlineStatsForClient(VpnClientBase client, Dictionary<string, ClientOnlineStats> stats)
    {
        string id = client switch
        {
            WireGuardClient wg => wg.PublicKey,
            AmneziaWgClient awg => awg.PublicKey,
            VlessClient vless => vless.Name,
            SocksClient socks => socks.Name,
            ShadowsocksClient ss => ss.Name,

            _ => string.Empty
        };


        if (stats.TryGetValue(id, out var result))
            return result;

        return null;
    }

    public static ClientOnlineStats? GetOnlineStatsForClient(VpnClientBase client)
    {
        try
        {
            if (client is AmneziaWgClient awgClient)
            {
                var awg = VpnManager.GetType<AmneziaWg>();
                var allAwgStats = awg.GetOnlineStats();
                return allAwgStats.FirstOrDefault(s => s.ClientId == awgClient.PublicKey);
            }

            if (client is WireGuardClient wgClient)
            {
                var wg = VpnManager.GetType<WireGuard>();
                var allWgStats = wg.GetOnlineStats();
                return allWgStats.FirstOrDefault(s => s.ClientId == wgClient.PublicKey);
            }

            /*
            if (client is VlessClient vlessClient)
            {
                var xray = VpnManager.GetType<Xray>();
                var allXrayStats = xray.GetOnlineStats(true);

                return allXrayStats.FirstOrDefault(s => s.ClientId.Equals(vlessClient.Name, StringComparison.OrdinalIgnoreCase));
            }
            */
        }
        catch (Exception ex)
        {
            Logger.Warn($"Failed to fetch online stats for client {client.Name}: {ex.Message}");
        }

        return null;
    }
}