public interface IFileManager
{
    bool TrySaveFile(string path, string content);
    bool IsContentChanged(string path, string currentContent);
    string CalculateHash(string content);

    bool Exists(params string[] paths);
    void Delete(params string[] paths);
}