public class TokensHandler : IHandler
{
    public bool CanHandle(InputContext input)
    {
        // token add --name --role
        // token del --name
        // token list

        if (input.Count != 2) return false;
        if (input.Args[0].ToLower() != "token") return false;

        var arg1 = input.Args[1].ToLower();

        var isAdd = arg1 == "add";
        var isDel = arg1 == "del";
        var isList = arg1 == "list";

        return isAdd || isDel || isList;
    }

    public void Handle(InputContext input)
    {
        var action = input.Args[1].ToLower();

        if (action == "add")
        {
            var name = input.GetFlagValue<NameFlag>();
            if (string.IsNullOrEmpty(name))
            {
                Logger.Warn($"Name requered for add.");
                return;
            }

            var roleStr = input.GetFlagValue<RoleFlag>()?.ToLower();
            if (string.IsNullOrEmpty(roleStr))
            {
                Logger.Warn($"Role requered for add.");
                return;
            }

            AccessLevel? level = null;
            if (roleStr == "admin")
            {
                level = AccessLevel.ADMIN;
            }
            else if (roleStr == "user")
            {
                level = AccessLevel.USER;
            }
            else if (roleStr == "mod" || roleStr == "moderator")
            {
                level = AccessLevel.MODERATOR;
            }

            if (level == null)
            {
                Logger.Warn($"Use only this role: admin, user, mod/moderator.");
                return;
            }

            var response = ApiClient.Current.CreateToken(name, level.Value);
            Logger.Info($"Secret for {name}: {response.Secret}");
        }
        else if (action == "del")
        {
            var name = input.GetFlagValue<NameFlag>();
            if (string.IsNullOrEmpty(name))
            {
                Logger.Warn($"Name requered for del.");
                return;
            }

            ApiClient.Current.DeleteToken(name);
            
            Logger.Info($"Token {name} was deleted.");
        }
        else if (action == "list")
        {
            var tokens = ApiClient.Current.GetTokens().Tokens;
            PrintList(tokens, input.HasFlag<WatchFlag>());
        }
    }

    private void PrintList(List<AuthTokenNetData> tokens, bool isWatch)
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
            view.WriteLine("================================================================================");
            view.WriteLine($"{"NAME",-16} {"LEVEL",-12} {"CREATED",-20} {"LAST USED",-15}");
            view.WriteLine("--------------------------------------------------------------------------------");

            foreach (var token in tokens.OrderByDescending(x => x.Level))
            {
                var created = token.CreatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm");

                var lastUsed = token.LastUsedAt.HasValue ? FormatManager.GetRelativeTime(token.LastUsedAt.Value) : "Never";

                view.WriteLine($"{token.Name,-16} {token.Level,-12} {created,-20} {lastUsed,-15}");
            }

            view.WriteLine("================================================================================");

            view.Wait();
        }
    }
}