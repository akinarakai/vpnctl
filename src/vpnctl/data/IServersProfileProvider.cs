public interface IServersProfileProvider
{
    ProfileStorage GetStorage();

    void Add(ServerProfile profile);
    void Remove(string name);
    ServerProfile? Get(string name, List<ServerProfile>? profiles = null);

    void SetCurrent(string name);
    ServerProfile? GetCurrent();

    void TrySave();
}