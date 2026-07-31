using System.Text.Json.Serialization;

[JsonDerivedType(typeof(WireGuardClient), typeDiscriminator: "wg")]
[JsonDerivedType(typeof(AmneziaWgClient), typeDiscriminator: "awg")]
[JsonDerivedType(typeof(VlessClient), typeDiscriminator: "vless")]
[JsonDerivedType(typeof(SocksClient), typeDiscriminator: "socks")]
[JsonDerivedType(typeof(ShadowsocksClient), typeDiscriminator: "ss")]
public abstract class VpnClientBase
{
    public long Id { get; set; } = 1;
    public string Name { get; init; } = string.Empty;
    public string ConfigStr { get; init; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    public ClientNetData ToNet(ProtocolType proto, ClientOnlineStats? stats = null)
    {
        return new ClientNetData
        {
            Protocol = proto,
            Id = Id,
            Name = Name,
            ConfigStr = ConfigStr,
            IsActive = IsActive,
            CreatedAt = CreatedAt,
            Stats = stats,
        };
    }
}

public class WireGuardClient : VpnClientBase
{
    public string PrivateKey { get; init; } = string.Empty;
    public string PublicKey { get; init; } = string.Empty;
    public string AllowedIp { get; init; } = string.Empty; // 10.0.0.2
}

public class AmneziaWgClient : VpnClientBase
{
    public string PrivateKey { get; init; } = string.Empty;
    public string PublicKey { get; init; } = string.Empty;
    public string AllowedIp { get; init; } = string.Empty; // 10.8.0.2
}

public class VlessClient : VpnClientBase
{
    public string Uuid { get; init; } = string.Empty;
    public string ShortId { get; init; } = string.Empty;
}

public class SocksClient : VpnClientBase
{
    public string Password { get; init; } = string.Empty;
}

public class ShadowsocksClient : VpnClientBase
{
    public string Password { get; init; } = string.Empty;
    public string Method { get; init; } = string.Empty;
}