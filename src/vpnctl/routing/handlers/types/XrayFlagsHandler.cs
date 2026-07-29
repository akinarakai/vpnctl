public class XrayFlagsHandler : IHandler
{
    public bool CanHandle(InputContext input)
    {
        if (input.Count != 1) return false;

        var command = input.Args[0].ToLower();

        // var isXray = command == "xray" && input.HasFlag<LogsFlag>();
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
        var xray = VpnManager.GetType<Xray>();
        if (xray.GetInstallStatus() == VpnInstallStatus.NOT_INSTALLED)
        {
            Logger.Warn("Xray is not installed.");
            return;
        }

        var proto = input.Args[0];

        bool needRestart = false;
        foreach (var flag in input.Flags)
        {
            /*
            if (proto == "xray")
            {
                if (flag.Value is LogsFlag)
                {
                    var result = Kernel.Cmd.Run("journalctl", "-u xray.service -n 20 --no-pager", true);
                    if (!result.Success)
                    {
                        throw new Exception("Failed to fetch system logs");
                    }

                    Logger.Text(result.Text.Trim());
                }
            }
            */
            if (proto == "reality")
            {
                if (flag.Value is KeysFlag)
                {
                    Logger.Info($"Start gen keys for Reality...");

                    var isForce = input.HasFlag<ForceFlag>();
                    if (xray.GenerateRealityKeys(isForce))
                    {
                        needRestart = true;
                        Logger.Success($"Reality successfully generate keys");
                    }
                }
            }
            else if (proto == "vless")
            {
                var server = Kernel.Data.GetServerState();

                if (flag.Value is UuidFlag)
                {
                    Logger.Info($"Start gen default uuid for Vless...");
                    if (xray.GenerateDefaultUuid())
                    {
                        needRestart = true;
                        Logger.Success($"Vless successfully generate uuid");
                    }
                }
                else if (flag.Value is SniFlag)
                {
                    if (flag.Arguments != null && flag.Arguments.Count > 0)
                    {
                        server.Xray.Vless.Sni = flag.Arguments[0];

                        needRestart = true;
                        Logger.Success($"Vless successfully set new sni {flag.Arguments[0]}");
                    }
                    else
                    {
                        Logger.Warn($"Sni flag cant be empty!");
                    }
                }
                else if (flag.Value is FingerprintFlag)
                {
                    if (flag.Arguments != null && flag.Arguments.Count > 0)
                    {
                        server.Xray.Vless.Fingerprint = flag.Arguments[0];

                        needRestart = true;
                        Logger.Success($"Vless successfully set new fingerprint {flag.Arguments[0]}");
                    }
                    else
                    {
                        Logger.Warn($"Fingerprint flag cant be empty!");
                    }
                }
                else if (flag.Value is SecurityFlag)
                {
                    if (flag.Arguments != null && flag.Arguments.Count > 0)
                    {
                        var securityStr = flag.Arguments[0];
                        if (securityStr != "none" && securityStr != "reality" && securityStr != "tls")
                        {
                            Logger.Warn($"Unsupported security type {securityStr}");
                            return;
                        }

                        server.Xray.Vless.Security = securityStr;

                        needRestart = true;
                        Logger.Success($"Vless successfully set new security {securityStr}");
                    }
                    else
                    {
                        Logger.Warn($"Fingerprint flag cant be empty!");
                    }
                }
            }
        }

        if (needRestart)
            xray.Restart();
    }
}