public interface IInputFlag
{
    string Name { get; }
    string? ShortName => null;
    int ArgumentCount { get; }
    string Description => "";
}

public class InitFlag : IInputFlag
{
    public string Name => "init";
    public int ArgumentCount => 0;
    public string Description => "Initialize the server configuration and state data";
}

public class StatusFlag : IInputFlag
{
    public string Name => "status";
    public int ArgumentCount => 0;
    public string Description => "Display the installation and active status of all VPN engines";
}

public class HelpFlag : IInputFlag
{
    public string Name => "help";
    public int ArgumentCount => 0;
    public string Description => "Display general or command-specific help information";
}

public class InstallFlag : IInputFlag
{
    public string Name => "install";
    public string? ShortName => "i";
    public int ArgumentCount => 0;
    public string Description => "Install the specified VPN provider component";
}

public class UninstallFlag : IInputFlag
{
    public string Name => "uninstall";
    public string? ShortName => "u";
    public int ArgumentCount => 0;
    public string Description => "Remove the specified VPN provider from the system";
}

public class RestartFlag : IInputFlag
{
    public string Name => "restart";
    public string? ShortName => "r";
    public int ArgumentCount => 0;
    public string Description => "Restart the specified VPN provider service daemon";
}

public class ShowFlag : IInputFlag
{
    public string Name => "show";
    public string? ShortName => "s";
    public int ArgumentCount => 0;
    public string Description => "Get the detailed runtime statistics and status of the provider";
}

public class UpFlag : IInputFlag
{
    public string Name => "up";
    public int ArgumentCount => 0;
    public string Description => "Enable network interface and start the provider connection";
}

public class DownFlag : IInputFlag
{
    public string Name => "down";
    public int ArgumentCount => 0;
    public string Description => "Disable network interface and stop the provider connection";
}

public class ForceFlag : IInputFlag
{
    public string Name => "force";
    public string? ShortName => "f";
    public int ArgumentCount => 0;
    public string Description => "Force execution by bypassing checks (e.g., overwriting existing keys)";
}

public class KeysFlag : IInputFlag
{
    public string Name => "keys";
    public int ArgumentCount => 0;
    public string Description => "Generate or rotate cryptographic keypairs for the connection";
}

public class UuidFlag : IInputFlag
{
    public string Name => "uuid";
    public int ArgumentCount => 0;
    public string Description => "Generate a new custom user identifier (UUID) for protocols like VLESS";
}

public class QrFlag : IInputFlag
{
    public string Name => "qr";
    public int ArgumentCount => 0;
    public string Description => "Generate and render a terminal-friendly QR code for client profiles";
}

public class CfgFlag : IInputFlag
{
    public string Name => "cfg";
    public int ArgumentCount => 0;
    public string Description => "Generate and render a terminal-friendly QR code for client profiles";
}

public class PasswordFlag : IInputFlag
{
    public string Name => "password";
    public string? ShortName => "pwd";
    public int ArgumentCount => 1;
    public string Description => "Set or pass a custom password/preshared key for the connection";
}

public class ShortIdFlag : IInputFlag
{
    public string Name => "short-id";
    public string? ShortName => "sid";
    public int ArgumentCount => 0;
    public string Description => "Generate a short hexagonal identifier for REALITY configuration";
}

public class SniFlag : IInputFlag
{
    public string Name => "sni";
    public int ArgumentCount => 1;
    public string Description => "Specify a custom Server Name Indication (SNI) host for TLS filtering";
}

public class FingerprintFlag : IInputFlag
{
    public string Name => "finger";
    public string? ShortName => "fp";
    public int ArgumentCount => 1;
    public string Description => "Set the client TLS fingerprint simulation profile (e.g., chrome, firefox)";
}

public class LogsFlag : IInputFlag
{
    public string Name => "logs";
    public string? ShortName => "l";
    public int ArgumentCount => 0;
    public string Description => "Stream real-time diagnostic log outputs from the provider service";
}

public class ObfuscateFlag : IInputFlag
{
    public string Name => "obfuscate";
    public string? ShortName => "obf";
    public int ArgumentCount => 0;
    public string Description => "Generate random advanced obfuscation (scrambling) metrics for AmneziaWG";
}

public class SecurityFlag : IInputFlag
{
    public string Name => "security";
    public int ArgumentCount => 1;
    public string Description => "Configure the encryption or transport security type (e.g., reality, none)";
}

public class RandomPortFlag : IInputFlag
{
    public string Name => "rand-port";
    public string? ShortName => "rp";
    public int ArgumentCount => 0;
    public string Description => "Randomize the server binding port using non-privileged dynamic ranges";
}

public class LinesFlag : IInputFlag
{
    public string Name => "lines";
    public string? ShortName => "n";
    public int ArgumentCount => 1;
    public string Description => "Lines";
}

public class PurgeFlag : IInputFlag
{
    public string Name => "purge";
    public int ArgumentCount => 0;
    public string Description => "Delete";
}

public class NameFlag : IInputFlag
{
    public string Name => "name";
    public int ArgumentCount => 1;
    public string Description => "Name";
}