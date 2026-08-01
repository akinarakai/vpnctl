public class XrayFlagsHandler : IHandler
{
    public bool CanHandle(InputContext input)
    {
        if (input.Count != 1) return false;

        var command = input.Args[0].ToLower();

        var isReality = command == "reality" && input.HasFlag<KeysFlag>();
        var isVless = command == "vless" && (
            input.HasFlag<UuidFlag>() ||
            input.HasFlag<SecurityFlag>() ||
            input.HasFlag<SniFlag>() ||
            input.HasFlag<FingerprintFlag>()
        );

        return isReality || isVless;
    }

    public void Handle(InputContext input)
    {
        var proto = input.Args[0];

        bool needRestart = false;
        foreach (var flag in input.Flags)
        {
            if (proto == "reality")
            {
                if (flag.Value is KeysFlag)
                {
                    Logger.Info($"Start gen keys for Reality...");

                    ApiClient.Current.SendProtocolAction(ProtocolType.VLESS, ProtocolNetActionType.GEN_KEYS);

                    Logger.Success($"Reality successfully generate keys");
                    needRestart = true;
                }
            }
            else if (proto == "vless")
            {
                if (flag.Value is UuidFlag)
                {
                    Logger.Info($"Start gen default uuid for Vless...");

                    ApiClient.Current.SendProtocolAction(ProtocolType.VLESS, ProtocolNetActionType.GEN_UUID);

                    Logger.Success($"Vless successfully generate uuid");
                    needRestart = true;
                }
                else if (flag.Value is SniFlag)
                {
                    if (flag.Arguments?.Count > 0)
                    {
                        var value = flag.Arguments[0];
                        ApiClient.Current.SendProtocolAction(ProtocolType.VLESS, ProtocolNetActionType.SET_SNI, value);

                        Logger.Success($"Vless successfully set new sni {value}");
                        needRestart = true;
                    }
                    else
                    {
                        Logger.Warn($"Sni flag cant be empty!");
                    }
                }
                else if (flag.Value is FingerprintFlag)
                {
                    if (flag.Arguments?.Count > 0)
                    {
                        var value = flag.Arguments[0];
                        ApiClient.Current.SendProtocolAction(ProtocolType.VLESS, ProtocolNetActionType.SET_FINGERPRINT, value);

                        Logger.Success($"Vless successfully set new fingerprint {value}");
                        needRestart = true;
                    }
                    else
                    {
                        Logger.Warn($"Fingerprint flag cant be empty!");
                    }
                }
                else if (flag.Value is SecurityFlag)
                {
                    if (flag.Arguments?.Count > 0)
                    {
                        var value = flag.Arguments[0];
                        ApiClient.Current.SendProtocolAction(ProtocolType.VLESS, ProtocolNetActionType.SET_SECURITY, value);

                        Logger.Success($"Vless successfully set new security {value}");
                        needRestart = true;
                    }
                    else
                    {
                        Logger.Warn($"Security flag cant be empty!");
                    }
                }
            }
        }

        if (needRestart)
            ApiClient.Current.SendVpnAction(VpnServiceType.XRAY, VpnNetActionType.RESTART);
    }
}