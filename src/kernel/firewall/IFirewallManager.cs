public interface IFirewallManager
{
    bool OpenUdp(int port);
    bool CloseUdp(int port);

    bool OpenTcp(int port);
    bool CloseTcp(int port);

    //bool IsOpenUdp(int port);
    //bool IsOpenTcp(int port);
}