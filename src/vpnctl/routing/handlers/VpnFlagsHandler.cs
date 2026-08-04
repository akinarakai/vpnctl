public class VpnFlagsHandler : IHandler
{
    public bool CanHandle(InputContext input)
    {
        return input.Count == 1 &&
        (input.HasFlag<InstallFlag>() ||
        input.HasFlag<UninstallFlag>() ||
        input.HasFlag<RestartFlag>() ||
        input.HasFlag<ShowFlag>() ||
        input.HasFlag<InitFlag>() ||
        input.HasFlag<UpFlag>() ||
        input.HasFlag<DownFlag>());
    }

    public void Handle(InputContext input)
    {
        var name = input.Args[0];
        var vpn = FormatManager.GetVpnTypeFromShortName(name);

        foreach (var flag in input.Flags)
        {
            if (flag.Value is InstallFlag)
            {
                Logger.Info($"Trying install {vpn}.");

                ApiClient.Current.SendVpnAction(vpn, VpnNetActionType.INSTALL);

                Logger.Success($"{vpn} successful installed.");
            }
            else if (flag.Value is UninstallFlag)
            {
                Logger.Info($"Trying uninstall {vpn}.");

                ApiClient.Current.SendVpnAction(vpn, VpnNetActionType.UNINSTALL);

                Logger.Success($"{vpn} successful uninstalled.");
            }
            else if (flag.Value is RestartFlag)
            {   
                Logger.Info($"Trying restart {vpn}.");

                ApiClient.Current.SendVpnAction(vpn, VpnNetActionType.RESTART);

                Logger.Success($"{vpn} successful restarted.");
            }
            else if (flag.Value is UpFlag)
            {
                Logger.Info($"Trying up {vpn}.");

                ApiClient.Current.SendVpnAction(vpn, VpnNetActionType.UP);

                Logger.Success($"{vpn} now active.");
            }
            else if (flag.Value is DownFlag)
            {
                Logger.Info($"Trying down {vpn}.");

                ApiClient.Current.SendVpnAction(vpn, VpnNetActionType.DOWN);

                Logger.Success($"{vpn} now down.");
            }
            else if (flag.Value is InitFlag)
            {
                Logger.Warn("If keys already exist on the server, all existing connections will be reset.\n" + "Do you want to continue? (y/n)");

                var answer = Console.ReadLine();

                if (answer?.ToLower() != "y")
                {
                    Logger.Info("Operation cancelled.");
                    return;
                }

                Logger.Info($"Trying init {vpn}.");

                ApiClient.Current.SendVpnAction(vpn, VpnNetActionType.INIT);

                Logger.Success($"{vpn} successful initialized.");
            }
            else if (flag.Value is LogsFlag)
            {
                //ApiClient.Get().SendVpnAction(vpn, VpnActionType.RESTART);
            }
            else if (flag.Value is ShowFlag)
            {
                //HandleShow(vpn, name);
            }
        }
    }

    /*
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
    */
}