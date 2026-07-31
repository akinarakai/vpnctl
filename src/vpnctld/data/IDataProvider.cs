public interface IDataProvider
{
    ClientsData GetClientsState();
    ServerData GetServerState();

    void DeleteFiles();
    void TrySave();

    VpnClientBase? GetClient(string name);
    bool IsNameExist(string name);
}