public interface INetworkManager
{
    string GetActiveNetInterface();
    string GetIP();

    bool InterfaceExists(string name);

    void CreateInterface(string name, string type);
    void DeleteInterface(string name);

    void SetInterfaceUp(string name);
    void SetInterfaceDown(string name);

    void AddAddress(string interfaceName, string address);
    void RemoveAddress(string interfaceName, string address);

    void AddRoute(string destination, string interfaceName);
    void RemoveRoute(string destination);

    void SetMtu(string interfaceName, int mtu);
}