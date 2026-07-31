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
                ApiClient.Get().SendVpnAction(vpn, VpnNetActionType.INSTALL);
            }
            else if (flag.Value is UninstallFlag)
            {
                ApiClient.Get().SendVpnAction(vpn, VpnNetActionType.UNINSTALL);
            }
            else if (flag.Value is RestartFlag)
            {
                ApiClient.Get().SendVpnAction(vpn, VpnNetActionType.RESTART);
            }
            else if (flag.Value is UpFlag)
            {
                ApiClient.Get().SendVpnAction(vpn, VpnNetActionType.UP);
            }
            else if (flag.Value is DownFlag)
            {
                ApiClient.Get().SendVpnAction(vpn, VpnNetActionType.DOWN);
            }
            else if (flag.Value is InitFlag)
            {
                ApiClient.Get().SendVpnAction(vpn, VpnNetActionType.INIT);
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