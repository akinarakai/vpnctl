public class ServersProfileHandler : IHandler
{
    public bool CanHandle(InputContext input)
    {
        // server switch --name
        // server add --name --adr --token --port
        // server del --name
        // server list
        // server info -- name
        // server upd --name --adr --token --port

        if (input.Count != 2) return false;
        if (input.Args[0].ToLower() != "server") return false;

        var arg1 = input.Args[1].ToLower();

        var isSwitch = arg1 == "switch";
        var isAdd = arg1 == "add";
        var isDel = arg1 == "del";
        var isList = arg1 == "list";
        var isUpdate = arg1 == "upd";
        var isInfo = arg1 == "info";

        return isSwitch || isAdd || isDel || isUpdate || isList || isInfo;
    }

    public void Handle(InputContext input)
    {
        var action = input.Args[1].ToLower();
        var provider = Kernel.Get<IServersProfileProvider>();

        if (action == "switch")
        {
            var name = input.GetFlagValue<NameFlag>();
            if (string.IsNullOrEmpty(name))
            {
                Logger.Warn($"Please use (--name name) to use switch");
                return;
            }

            Logger.Info($"Trying switching to {name}");

            provider.SetCurrent(name);

            Logger.Success($"Current server switching!");
        }
        else if (action == "add")
        {
            var name = input.GetFlagValue<NameFlag>();
            if (string.IsNullOrEmpty(name))
            {
                Logger.Warn($"Please use (--name name) to use add");
                return;
            }

            Logger.Info($"Trying add server profile.");

            var portValue = input.GetFlagValue<PortFlag>();

            var server = new ServerProfile
            {
                Name = name,
                Token = input.GetFlagValue<TokenFlag>() ?? string.Empty,
                Port = int.TryParse(portValue, out var port) ? port : 0,
                Address = input.GetFlagValue<AddressFlag>() ?? string.Empty,
            };

            provider.Add(server);

            Logger.Success($"New server added!");
        }
        else if (action == "del")
        {
            var name = input.GetFlagValue<NameFlag>();
            if (string.IsNullOrEmpty(name))
            {
                Logger.Warn($"Please use (--name name) to use del");
                return;
            }

            Logger.Info($"Trying delete server profile.");

            provider.Remove(name);

            Logger.Success($"Server profile {name} deleted!");
        }
        else if (action == "list")
        {
            var storage = provider.GetStorage();
            if (storage.Profiles.Count == 0)
            {
                Logger.Warn("No server profiles found.");
                return;
            }

            PrintProfiles(storage, input.HasFlag<WatchFlag>());
        }
        else if (action == "upd")
        {
            var name = input.GetFlagValue<NameFlag>();

            if (string.IsNullOrEmpty(name))
            {
                Logger.Warn("Please use (--name name) to update server");
                return;
            }

            var profile = provider.Get(name);
            if (profile == null)
            {
                Logger.Warn($"Server '{name}' not found");
                return;
            }

            var address = input.GetFlagValue<AddressFlag>();
            if (!string.IsNullOrEmpty(address))
            {
                profile.Address = address;
                Logger.Success($"Server address updated to: {address}");
            }

            var portValue = input.GetFlagValue<PortFlag>();
            if (int.TryParse(portValue, out var port))
            {
                profile.Port = port;
                Logger.Success($"Server port updated to: {port}");
            }

            var token = input.GetFlagValue<TokenFlag>();
            if (!string.IsNullOrEmpty(token))
            {
                profile.Token = token;
                Logger.Success($"Server token updated");
            }

            Logger.Info($"Server '{name}' updated");
        }
        else if (action == "info")
        {
            var name = input.GetFlagValue<NameFlag>();
            var isWatch = input.HasFlag<WatchFlag>();

            if (!string.IsNullOrEmpty(name))
            {
                var profile = provider.Get(name);
                if (profile == null)
                {
                    Logger.Warn($"Server profile: {name} not found!");
                    return;
                }

                PrintServerInfo(profile, isWatch);
            }
            else
            {
                var profile = provider.GetCurrent();
                if (profile == null)
                {
                    Logger.Warn($"Current server profile: {name} not found!");
                    return;
                }

                PrintServerInfo(profile, isWatch);
            }
        }
    }

    private void PrintProfiles(ProfileStorage storage, bool isWatch)
    {
        ConsoleLiveView view;
        if (isWatch)
        {
            view = new ConsoleLiveView();
        }
        else
        {
            view = new ConsoleLiveView(1);
        }

        view.Start();

        while (view.KeepRunning())
        {
            view.WriteLine("==============================================================================================");
            view.WriteLine($"  {"NAME",-15} {"STATUS",-10} {"ENDPOINT",-20} {"LATENCY",-8} {"UPTIME",-15} {"HOSTNAME",-15}");
            view.WriteLine("----------------------------------------------------------------------------------------------");

            foreach (var profile in storage.Profiles)
            {
                try
                {
                    var isCurrent = profile.Name.Equals(storage.CurrentProfile, StringComparison.OrdinalIgnoreCase);

                    var client = ApiClient.Create(profile);
                    var isOnline = client.TryGetServerInfo(out var info);

                    var name = $"{(isCurrent ? "*" : " ")} {profile.Name}";

                    var status = isOnline ? "● ONLINE" : "○ OFFLINE";

                    var latency = isOnline && info != null ? $"{info.LatencyMs}ms" : "-";

                    var uptime = isOnline && info != null ? FormatManager.GetUptime(info.Response.Uptime) : "-";

                    var hostname = isOnline && info != null ? info.Response.Hostname : "-";
                    var endpoint = $"{profile.Address}:{profile.Port}";

                    view.WriteLine($"  {name,-15} {status,-10} {endpoint,-20} {latency,-8} {uptime,-15} {hostname,-15}");
                }
                catch (Exception ex)
                {
                    view.WriteLine($"Server error '{profile.Name}': {ex.Message}");
                }
            }

            view.WriteLine("==============================================================================================");

            view.Wait();
        }
    }

    private void PrintServerInfo(ServerProfile profile, bool isWatch)
    {
        var client = ApiClient.Create(profile);
        var isOnline = client.TryGetServerInfo(out var info) && info != null;

        ConsoleLiveView view;
        if (isWatch)
        {
            view = new ConsoleLiveView();
        }
        else
        {
            view = new ConsoleLiveView(1);
        }

        view.Start();

        while (view.KeepRunning())
        {
            view.WriteLine("================================================================================");
            view.WriteLine($" SERVER: {profile.Name}");
            view.WriteLine("--------------------------------------------------------------------------------");
            view.WriteLine($" ENDPOINT: {profile.Address}:{profile.Port}");
            view.WriteLine($" STATUS:   {(isOnline ? "● ONLINE" : "○ OFFLINE")}");
            view.WriteLine($" TOKEN:    {(string.IsNullOrEmpty(profile.Token) ? "NOT SET" : "CONFIGURED")}");

            if (isOnline)
            {
                view.WriteLine($" LATENCY:  {info!.LatencyMs}ms");
                view.WriteLine($" HOSTNAME: {info.Response.Hostname}");
                view.WriteLine($" OS:       {info.Response.Os}");
                view.WriteLine($" ARCH:     {info.Response.Arch}");
                view.WriteLine($" UPTIME:   {FormatManager.GetUptime(info.Response.Uptime)}");
                view.WriteLine($" UTC:      {info.Response.UtcTime:yyyy-MM-dd HH:mm:ss}");
            }

            view.WriteLine("================================================================================");

            view.Wait();
        }
    }
}