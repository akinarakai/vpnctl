using System.Text;
using System.Diagnostics;

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

static void Install()
{
    Console.WriteLine("Starting vpnctld setup...");
    
    var currentDir = AppContext.BaseDirectory;

    var sourceBinary = Path.Combine(currentDir, "vpnctld");
    var targetBinary = "/usr/local/bin/vpnctld";
    var servicePath = "/etc/systemd/system/vpnctld.service";

    Console.WriteLine("[INFO] Starting vpnctld installation...");
    Console.WriteLine($"[INFO] Source directory: {currentDir}");

    if (!File.Exists(sourceBinary))
    {
        Console.WriteLine($"[ERROR] Binary not found: {sourceBinary}");
        return;
    }

    Console.WriteLine($"[INFO] Copying binary:");
    Console.WriteLine($"       {sourceBinary}");
    Console.WriteLine($"       -> {targetBinary}");

    File.Copy(sourceBinary, targetBinary, true);

    Console.WriteLine("[OK] Binary copied.");

    Console.WriteLine("[INFO] Setting executable permission...");
    Run("chmod", $"+x {targetBinary}");
    Console.WriteLine("[OK] Permission set.");

    Console.WriteLine("[INFO] Creating systemd service...");

    var service = new StringBuilder();

    service.AppendLine("[Unit]");
    service.AppendLine("Description=vpnctl daemon");
    service.AppendLine("After=network.target");
    service.AppendLine();
    service.AppendLine("[Service]");
    service.AppendLine("Type=simple");
    service.AppendLine($"ExecStart={targetBinary}");
    service.AppendLine("Restart=always");
    service.AppendLine("RestartSec=5");
    service.AppendLine();
    service.AppendLine("[Install]");
    service.AppendLine("WantedBy=multi-user.target");

    File.WriteAllText(servicePath, service.ToString().Trim());

    Console.WriteLine($"[OK] Service created: {servicePath}");

    Console.WriteLine("[INFO] Reloading systemd...");
    Run("systemctl", "daemon-reload");
    Console.WriteLine("[OK] Systemd reloaded.");

    Console.WriteLine("[INFO] Enabling vpnctld service...");
    Run("systemctl", "enable vpnctld");
    Console.WriteLine("[OK] Service enabled.");

    Console.WriteLine("[INFO] Starting vpnctld service...");
    Run("systemctl", "start vpnctld");
    Console.WriteLine("[OK] Service started.");

    Console.WriteLine();
    Console.WriteLine("[SUCCESS] vpnctld installed successfully!");
    Console.WriteLine("Check status: systemctl status vpnctld");
}

static void Uninstall()
{
    Console.WriteLine("[INFO] Starting vpnctld removal...");

    Console.WriteLine("[INFO] Stopping service...");
    Run("systemctl", "stop vpnctld");

    Console.WriteLine("[INFO] Disabling service...");
    Run("systemctl", "disable vpnctld");

    var servicePath = "/etc/systemd/system/vpnctld.service";
    if (File.Exists(servicePath))
    {
        Console.WriteLine($"[INFO] Removing service file: {servicePath}");
        File.Delete(servicePath);
        Console.WriteLine("[OK] Service file removed.");
    }
    else
    {
        Console.WriteLine("[WARN] Service file not found.");
    }

    var binaryPath = "/usr/local/bin/vpnctld";
    if (File.Exists(binaryPath))
    {
        Console.WriteLine($"[INFO] Removing binary: {binaryPath}");
        File.Delete(binaryPath);
        Console.WriteLine("[OK] Binary removed.");
    }
    else
    {
        Console.WriteLine("[WARN] Binary not found.");
    }

    Console.WriteLine("[INFO] Reloading systemd...");
    Run("systemctl", "daemon-reload");

    Console.WriteLine();
    Console.WriteLine("[SUCCESS] vpnctld removed.");
    Console.WriteLine("[INFO] Configuration files were not deleted.");
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