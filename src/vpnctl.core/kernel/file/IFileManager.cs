public interface IFileManager
{
    bool TrySave(string path, string content);
    bool TryRead(string path, out string content);

    bool Copy(string from, string to);
    bool Exists(string path);
    void Delete(params string[] paths);
    void CreateDirectories(params string[] paths);

    bool IsContentChanged(string path, string currentContent);
    string CalculateHash(string content);
}