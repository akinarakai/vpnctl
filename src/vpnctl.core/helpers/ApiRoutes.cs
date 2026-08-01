public static class ApiRoutes
{
    public static class Server
    {
        public const string Info = "/server/info";
    }

    public static class System
    {
        public const string Monitor = "/system/monitor";
    }

    public static class Vpn
    {
        public const string List = "/vpn/list";
        public const string Logs = "/vpn/logs";
        public const string Action = "/vpn/action";
    }

    public static class Clients
    {
        public const string List = "/clients";
        public const string Action = "/clients/action";
    }

    public static class Protocols
    {
        public const string Action = "/protocols/action";
    }

    public static class Maintenance
    {
        public const string Purge = "/purge";
    }
}