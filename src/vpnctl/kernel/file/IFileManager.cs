public interface IFileManager
{
    bool TrySaveFile(string path, string content);
    bool IsContentChanged(string path, string currentContent);
    string CalculateHash(string content);

    bool Copy(string from, string to);
    bool Exists(params string[] paths);
    void Delete(params string[] paths);
    void CreateDirectories(params string[] paths);
}