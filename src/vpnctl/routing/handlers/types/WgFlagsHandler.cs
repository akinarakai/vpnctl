public class WgFlagsHandler : IHandler
{
    public bool CanHandle(InputContext input)
    {
        if (input.Count != 1) return false;

        var command = input.Args[0].ToLower();

        return command == "wg" && (input.HasFlag<KeysFlag>());
    }

    public void Handle(InputContext input)
    {
        var wg = VpnManager.GetType<WireGuard>();
        if (wg.GetInstallStatus() == VpnInstallStatus.NOT_INSTALLED)
        {
            Logger.Warn($"WireGuard is already uninstalled!");
            return;
        }

        bool needRestart = false;
        foreach (var flag in input.Flags)
        {
            if (flag.Value is KeysFlag)
            {
                Logger.Info($"Start gen keys for WireGuard...");

                var isForce = input.HasFlag<ForceFlag>();
                if (wg.GenerateKeys(isForce))
                {
                    needRestart = true;
                    Logger.Success($"WireGuard successfully generate keys");
                }
            }
        }

        if (needRestart)
            wg.Restart();
    }
}