using System.Text;
using System.Diagnostics;

static class Program
{
    const string VpnctlPath = "/usr/local/bin/vpnctl";
    const string VpnctldPath = "/usr/local/bin/vpnctld";
    const string ServicePath = "/etc/systemd/system/vpnctld.service";

    static string CurrentDir => AppContext.BaseDirectory;

    static string VpnctlSource => Path.Combine(CurrentDir, "vpnctl");
    static string VpnctldSource => Path.Combine(CurrentDir, "vpnctld");

    static void Main(string[] args)
    {
        if (Environment.UserName != "root")
        {
            Console.WriteLine("Error: run as root.");
            return;
        }

        if (args.Length == 0)
        {
            Console.WriteLine("Usage: vpnctl-setup -i | -u");
            return;
        }

        switch (args[0])
        {
            case "-i":
                Install();
                break;

            case "-u":
                Uninstall();
                break;

            default:
                Console.WriteLine("Unknown argument. Use -i for install or -u for uninstall.");
                break;
        }
    }

    static void Install()
    {
        Uninstall();

        Console.WriteLine("Starting vpnctl installation...");
        Console.WriteLine($"[INFO] Source directory: {CurrentDir}");

        // ---- vpnctl ----
        InstallBinary(VpnctlSource, VpnctlPath, "vpnctl", false);

        // ---- vpnctld ----
        InstallBinary(VpnctldSource, VpnctldPath, "vpnctld");

        // ---- systemd service ----
        Console.WriteLine("[INFO] Creating systemd service...");

        var service = new StringBuilder();

        service.AppendLine("[Unit]");
        service.AppendLine("Description=vpnctl daemon");
        service.AppendLine("After=network.target");
        service.AppendLine();
        service.AppendLine("[Service]");
        service.AppendLine("Type=simple");
        service.AppendLine($"ExecStart={VpnctldPath}");
        service.AppendLine("Restart=always");
        service.AppendLine("RestartSec=5");
        service.AppendLine();
        service.AppendLine("[Install]");
        service.AppendLine("WantedBy=multi-user.target");

        File.WriteAllText(ServicePath, service.ToString());

        Console.WriteLine("[OK] Service created.");

        Console.WriteLine("[INFO] Reloading systemd...");
        Run("systemctl", "daemon-reload");

        Console.WriteLine("[INFO] Enabling service...");
        Run("systemctl", "enable vpnctld");

        Console.WriteLine("[INFO] Starting service...");
        Run("systemctl", "start vpnctld");

        Console.WriteLine();
        Console.WriteLine("[SUCCESS] vpnctl installed successfully.");
        Console.WriteLine("You can now run:");
        Console.WriteLine("  vpnctl");
    }

    static void Uninstall()
    {
        Console.WriteLine("[INFO] Starting vpnctl removal...");

        Console.WriteLine("[INFO] Stopping service...");
        Run("systemctl", "stop vpnctld");

        Console.WriteLine("[INFO] Disabling service...");
        Run("systemctl", "disable vpnctld");

        DeleteIfExists(ServicePath, "service");

        Console.WriteLine("[INFO] Reloading systemd...");
        Run("systemctl", "daemon-reload");

        DeleteIfExists(VpnctldPath, "vpnctld");
        DeleteIfExists(VpnctlPath, "vpnctl");

        Console.WriteLine();
        Console.WriteLine("[SUCCESS] vpnctl removed.");
    }

    static void InstallBinary(string source, string target, string name, bool required = true)
    {
        Console.WriteLine($"[INFO] Installing {name}...");

        if (!File.Exists(source))
        {
            if (required)
                throw new FileNotFoundException($"Binary not found: {source}");

            Console.WriteLine($"[WARN] Binary not found: {source}");
            return;
        }

        File.Copy(source, target, true);
        Run("chmod", $"+x {target}");

        Console.WriteLine($"[OK] {name} installed.");
    }

    static void DeleteIfExists(string path, string name)
    {
        Console.WriteLine($"[INFO] Removing {name}...");

        if (File.Exists(path))
        {
            File.Delete(path);
            Console.WriteLine($"[OK] {name} removed.");
        }
        else
        {
            Console.WriteLine($"[WARN] {name} not found.");
        }
    }

    static void Run(string command, string args)
    {
        var process = Process.Start(new ProcessStartInfo
        {
            FileName = command,
            Arguments = args,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        });

        process?.WaitForExit();

        if (process?.ExitCode != 0)
        {
            Console.WriteLine(process?.StandardError.ReadToEnd());
        }
    }
}