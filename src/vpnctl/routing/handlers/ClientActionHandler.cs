public class ClientActionHandler : IHandler
{
    private static readonly HashSet<string> ReservedNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "add", "del", "list", "show", "remove", "client"
    };

    public bool CanHandle(InputContext input)
    {
        if (input.Count != 3) return false;

        var arg0 = input.Args[0].ToLower();
        var arg1 = input.Args[1].ToLower();
        var arg2 = input.Args[2].ToLower();

        // proto client add
        var isCreate = arg1 == "client" &&
                        arg2 == "add";
        // client del name
        var isDelete = arg0 == "client" &&
                        arg1 == "del";

        // client up/down name
        var isUpDown = arg0 == "client" &&
                        (arg1 == "up" || arg1 == "down");

        return isCreate || isDelete || isUpDown;
    }

    public void Handle(InputContext input)
    {
        var action = input.Args[0] == "client" ? input.Args[1] : input.Args[2];

        if (action == "add")
        {
            HandleCreate(input);
        }
        else if (action == "del")
        {
            HandleDelete(input);
        }
        else if (action == "up" || action == "down")
        {
            HandleActive(input, action);
        }
    }

    private void HandleActive(InputContext input, string action)
    {
        var clientName = input.Args[2];
        var actionType = action == "up" ? ClientNetActionType.UP : ClientNetActionType.DOWN;

        var request = new ClientActionRequest
        {
            Name = clientName,
            Action = actionType,
        };

        ApiClient.Get().SendClientAction(request);
    }

    private void HandleCreate(InputContext input)
    {
        string? clientName = null;

        if (input.TryGetFlag<NameFlag>(out var nameFlag) && nameFlag?.Arguments?.Count > 0)
        {
            clientName = nameFlag.Arguments[0];

            if (ReservedNames.Contains(clientName))
            {
                Logger.Warn($"Failed to create client: '{clientName}' is a reserved command keyword.");
                return;
            }
        }

        var proto = FormatManager.GetProtocolFromShortName(input.Args[0]);
        var needShortId = input.HasFlag<ShortIdFlag>();

        string? password = null;
        if (input.TryGetFlag<PasswordFlag>(out var pwdFlag) && pwdFlag?.Arguments?.Count > 0)
        {
            password = pwdFlag.Arguments[0];
        }

        var request = new ClientActionRequest
        {
            Protocol = proto,
            Name = clientName,
            NeedShortId = needShortId,
            Password = password,
            Action = ClientNetActionType.ADD,
        };

        var client = ApiClient.Get().SendClientAction(request);
        if (client != null)
        {
            Logger.Text($"--- CONFIG FOR {client.Name} ---");
            QRCode.Render(client.ConfigStr);
            Logger.Text("-------------------------------------");
        }
        else
        {
            Logger.Warn($"Failed create client");
        }
    }

    private void HandleDelete(InputContext input)
    {
        var clientName = input.Args[2];

        var request = new ClientActionRequest
        {
            Name = clientName,
            Action = ClientNetActionType.DEL,
        };

        ApiClient.Get().SendClientAction(request);
    }
}