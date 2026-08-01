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
        bool needRestart = false;
        foreach (var flag in input.Flags)
        {
            if (flag.Value is KeysFlag)
            {
                Logger.Info($"Start gen keys for WireGuard...");

                ApiClient.Current.SendProtocolAction(ProtocolType.WIREGUARD, ProtocolNetActionType.GEN_KEYS);

                Logger.Success($"WireGuard successfully generate keys");
                needRestart = true;
            }
        }

        if (needRestart)
            ApiClient.Current.SendVpnAction(VpnServiceType.WIREGUARD, VpnNetActionType.RESTART);
    }
}