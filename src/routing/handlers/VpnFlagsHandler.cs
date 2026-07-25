public class VpnFlagsHandler : IHandler
{
    public bool CanHandle(InputContext input)
    {
        return input.Count > 1 &&
        (input.HasFlag<InstallFlag>() ||
        input.HasFlag<UninstallFlag>() ||
        input.HasFlag<RestartFlag>() ||
        input.HasFlag<ShowFlag>() ||
        input.HasFlag<UpFlag>() ||
        input.HasFlag<DownFlag>());
    }

    public void Handle(InputContext input)
    {
        var name = input.Args[0];

        var vpn = VpnManager.Get(name);
        if (vpn == null)
        {
            Logger.Warn($"Supported core engine for \"{name}\" was not found.");
            return;
        }

        name = VpnHelper.GetNameFromType(vpn.Type);

        foreach (var flag in input.Flags)
        {
            if (flag.Value is InstallFlag)
            {
                if (CanContinue(vpn, VpnInstallStatus.INSTALLED, name, "cannon install"))
                {
                    Logger.Info("Updating system package repositories...");
                    var updateResult = Kernel.Cmd.Run("apt-get", "update -y", true, false);
                    if (!updateResult.Success)
                    {
                        Logger.Warn($"System update completed with warnings. {updateResult.Text.Trim()}");
                    }

                    Logger.Info($"Start installing {name}...");

                    Kernel.Data.GetServerState().NetworkInterface = Kernel.Network.GetActiveInterface();

                    var isForce = input.HasFlag<ForceFlag>();
                    if (vpn.Install(isForce))
                    {
                        Kernel.SysConfig.ApplySystemOptimizations();
                        vpn.Restart();
                        
                        Logger.Success($"{name} successfully installed!");
                    }
                    else Logger.Error($"Failed to install {name}.");
                }
            }
            else if (flag.Value is UninstallFlag)
            {
                if (CanContinue(vpn, VpnInstallStatus.NOT_INSTALLED, name, "cannon uninstall"))
                {
                    Logger.Info($"Start uninstalling {name}...");

                    if (vpn.Uninstall())
                    {
                        Logger.Success($"{name} successfully uninstalled!");
                    }
                    else Logger.Error($"Failed to uninstall {name}.");
                }
            }
            else if (flag.Value is RestartFlag)
            {
                if (CanContinue(vpn, VpnInstallStatus.NOT_INSTALLED, name, "cannon restart"))
                {
                    Logger.Info($"Restarting {name}...");

                    if (vpn.Restart())
                    {
                        Logger.Success($"{name} successfully restarted!");
                    }
                    else Logger.Error($"Failed to restart {name}.");
                }
            }
            else if (flag.Value is UpFlag)
            {
                if (CanContinue(vpn, VpnInstallStatus.NOT_INSTALLED, name, "cannon up"))
                {
                    if (vpn.GetActiveStatus() == VpnActiveStatus.ACTIVE)
                    {
                        Logger.Warn($"{name} is already active!");
                    }
                    else
                    {
                        Logger.Info($"Turning {name} service up...");

                        if (vpn.ToggleActive(true))
                        {
                            Logger.Success($"{name} is now up.");
                        }
                        else Logger.Error($"Failed to bring up {name}.");
                    }
                }
            }
            else if (flag.Value is DownFlag)
            {
                if (CanContinue(vpn, VpnInstallStatus.NOT_INSTALLED, name, "cannon down"))
                {
                    if (vpn.GetActiveStatus() == VpnActiveStatus.INACTIVE)
                    {
                        Logger.Warn($"{name} is already inactive!");
                    }
                    else
                    {
                        Logger.Info($"Turning {name} service down...");

                        if (vpn.ToggleActive(false))
                        {
                            Logger.Success($"{name} is now down.");
                        }
                        else Logger.Error($"Failed to bring down {name}.");
                    }
                }
            }
            else if (flag.Value is ShowFlag)
            {
                HandleShow(vpn, name);
            }
        }
    }

    private bool CanContinue(IVpnService vpn, VpnInstallStatus checkStatus, string name, string extra)
    {
        if (checkStatus == VpnInstallStatus.NOT_INSTALLED && vpn.GetInstallStatus() == VpnInstallStatus.NOT_INSTALLED)
        {
            Logger.Warn($"{name} is already uninstalled! {extra}");
            return false;
        }
        else if (checkStatus == VpnInstallStatus.INSTALLED && vpn.GetInstallStatus() == VpnInstallStatus.INSTALLED)
        {
            Logger.Warn($"{name} is already installed! {extra}");
            return false;
        }

        return true;
    }

    private void HandleShow(IVpnService vpn, string name)
    {
        var installStatus = vpn.GetInstallStatus();
        var activeStatus = vpn.GetActiveStatus();

        Console.WriteLine("==================================================");

        var stateText = activeStatus == VpnActiveStatus.ACTIVE ? "● ACTIVE" : "○ INACTIVE";
        if (installStatus == VpnInstallStatus.NOT_INSTALLED) stateText = "○ NOT INSTALLED";

        Console.WriteLine($"  Vpn Name:  {name}");
        Console.WriteLine($"  Install State:  {(installStatus == VpnInstallStatus.INSTALLED ? "● Installed" : "○ Absent")}");
        Console.WriteLine($"  Engine State:   {stateText}");
        Console.WriteLine();

        var info = vpn.GetInfo();
        if (string.IsNullOrEmpty(info))
        {
            Console.WriteLine("  [No Runtime Data Available]");
            Console.WriteLine("  Reason: The service engine is currently stopped or uninitialized.");
        }
        else
        {
            Console.WriteLine(info);
        }

        Console.WriteLine("==================================================");
    }
}