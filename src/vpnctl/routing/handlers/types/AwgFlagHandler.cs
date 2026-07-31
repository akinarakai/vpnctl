public class AwgFlagsHandler : IHandler
{
    public bool CanHandle(InputContext input)
    {
        if (input.Count != 1) return false;

        var command = input.Args[0].ToLower();

        return command == "awg" &&
        (input.HasFlag<KeysFlag>() ||
        input.HasFlag<RandomPortFlag>() ||
        input.HasFlag<ObfuscateFlag>());
    }

    public void Handle(InputContext input)
    {
        bool needRestart = false;
        foreach (var flag in input.Flags)
        {
            if (flag.Value is KeysFlag)
            {
                Logger.Info($"Start gen keys for AmneziaWG...");

                ApiClient.Get().SendProtocolAction(ProtocolType.AMNEZIAWG, ProtocolNetActionType.GEN_KEYS);

                Logger.Success($"AmneziaWG successfully generate keys.");
                needRestart = true;
            }
            else if (flag.Value is ObfuscateFlag)
            {
                Logger.Info($"Start randomize obfuscate AmneziaWG...");

                ApiClient.Get().SendProtocolAction(ProtocolType.AMNEZIAWG, ProtocolNetActionType.OBFUSCATE);

                Logger.Success($"AmneziaWG successfully obfuscate.");
                needRestart = true;
            }
            else if (flag.Value is RandomPortFlag)
            {
                Logger.Info($"Start randomize port AmneziaWG...");

                ApiClient.Get().SendProtocolAction(ProtocolType.AMNEZIAWG, ProtocolNetActionType.RANDOMIZE_PORT);

                Logger.Success($"AmneziaWG successfully port.");
                needRestart = true;
            }
        }

        if (needRestart)
            ApiClient.Get().SendVpnAction(VpnServiceType.AMNEZIAWG, VpnNetActionType.RESTART);
    }
}