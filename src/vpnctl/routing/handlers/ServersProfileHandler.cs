public class ServersProfileHandler : IHandler
{
    public bool CanHandle(InputContext input)
    {
        // server use --name
        // server add --name --adr --token --port
        // server del --name
        // server list
        // server info -- name
        // server upd --name --adr --token --port

        if (input.Count != 2) return false;
        if (input.Args[0].ToLower() != "server") return false;

        var arg1 = input.Args[1].ToLower();

        var isSwitch = arg1 == "use";
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

        if (action == "use")
        {
            var name = input.GetFlagValue<NameFlag>();
            if (string.IsNullOrEmpty(name))
            {
                Logger.Warn($"Please use (--name name) to use use");
                return;
            }

            Logger.Info($"Trying use server '{name}'");

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
            var table = new ConsoleTable() 
            {
                Width = 85
            };

            table.AddBorder();

            table.AddHeaders(
                new() { Name = "NAME", Spacing = 20 },
                new() { Name = "STATUS", Spacing = 10 },
                new() { Name = "ENDPOINT", Spacing = 20 },
                new() { Name = "LATENCY", Spacing = 8 },
                new() { Name = "UPTIME", Spacing = 8 },
                new() { Name = "HOSTNAME", Spacing = 15 }
            );

            table.AddSeparator();

            foreach (var profile in storage.Profiles)
            {
                try
                {
                    var isCurrent = profile.Name.Equals(storage.CurrentProfile, StringComparison.OrdinalIgnoreCase);

                    var client = ApiClient.Create(profile);
                    var isOnline = client.TryGetServerInfo(out var info);

                    var name = $"{profile.Name} {(isCurrent ? "✓" : " ")}";

                    var status = isOnline ? "● ONLINE" : "○ OFFLINE";

                    var latency = isOnline && info != null ? $"{info.LatencyMs}ms" : "-";

                    var uptime = isOnline && info != null ? FormatManager.GetUptime(info.Response.Uptime) : "-";

                    var hostname = isOnline && info != null ? info.Response.Hostname : "-";
                    var endpoint = $"{profile.Address}:{profile.Port}";

                    table.AddRow(name, status, endpoint, latency, uptime, hostname);
                }
                catch (Exception ex)
                {
                    table.AddText($"Server error '{profile.Name}': {ex.Message}");
                }
            }

            table.AddBorder();

            foreach (var line in table.Build())
            {
                view.WriteLine(line);
            }

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
            var table = new ConsoleTable();

            table.AddBorder();

            table.AddText($"SERVER: {profile.Name}");

            table.AddSeparator();

            table.AddText($"ENDPOINT: {profile.Address}:{profile.Port}");
            table.AddText($"STATUS:   {(isOnline ? "● ONLINE" : "○ OFFLINE")}");
            table.AddText($"TOKEN:    {(string.IsNullOrEmpty(profile.Token) ? "NOT SET" : "CONFIGURED")}");

            if (isOnline)
            {
                table.AddText($"LATENCY:  {info!.LatencyMs}ms");
                table.AddText($"HOSTNAME: {info.Response.Hostname}");
                table.AddText($"OS:       {info.Response.Os}");
                table.AddText($"ARCH:     {info.Response.Arch}");
                table.AddText($"UPTIME:   {FormatManager.GetUptime(info.Response.Uptime)}");
                table.AddText($"UTC:      {info.Response.UtcTime:yyyy-MM-dd HH:mm:ss}");
            }

            table.AddBorder();

            foreach (var line in table.Build())
            {
                view.WriteLine(line);
            }

            view.Wait();
        }
    }
}