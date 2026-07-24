public interface INetworkManager
{
    void EnableIPv4Forwarding();
    void EnableIPv6Forwarding();

    string GetIP();
}