public interface IDataProvider
{
    ClientsData GetClientsState();
    ServerData GetServerState();
    List<AuthToken> GetTokens();

    void DeleteFiles();
    void TrySave();

    VpnClientBase? GetClient(string name);
    AuthToken? GetToken(string token);

    void AddToken(string name, AccessLevel level, out string secret);
    void RemoveToken(string name);
}