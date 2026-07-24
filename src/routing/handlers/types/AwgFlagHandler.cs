public class AwgFlagsHandler : IHandler
{
    public bool CanHandle(InputContext input)
    {
        return input.Count > 1 && input.Args[0] == "awg" && (input.HasFlag<KeysFlag>() || input.HasFlag<RandomPortFlag>() || input.HasFlag<ObfuscateFlag>());
    }

    public void Handle(InputContext input)
    {
        var awg = VpnManager.GetType<AmneziaWg>();
        if (awg.GetInstallStatus() == VpnInstallStatus.NOT_INSTALLED)
        {
            Logger.Warn($"AmneziaWG is already uninstalled!");
            return;
        }

        bool needRestart = false;
        foreach (var flag in input.Flags)
        {
            if (flag.Value is KeysFlag)
            {
                Logger.Info($"Start gen keys for AmneziaWG...");

                var isForce = input.HasFlag<ForceFlag>();
                if (awg.GenerateServerKeys(isForce))
                {
                    needRestart = true;
                    Logger.Success($"AmneziaWG successfully generate keys.");
                }
            }
            else if (flag.Value is ObfuscateFlag)
            {
                Logger.Info($"Start randomize obfuscate AmneziaWG...");

                if (awg.GenerateObfuscation())
                {
                    needRestart = true;
                    Logger.Success($"AmneziaWG successfully obfuscate.");
                }
            }
            else if (flag.Value is RandomPortFlag)
            {
                Logger.Info($"Start randomize port AmneziaWG...");

                if (awg.RandomizePort())
                {
                    needRestart = true;
                    Logger.Success($"AmneziaWG successfully port.");
                }
            }
        }

        if (needRestart)
            awg.Restart();
    }
}