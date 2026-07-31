public static class TextParser
{
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
}