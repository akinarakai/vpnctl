using System.Security.Cryptography;
using System.Text;

public class BaseFileManager : IFileManager
{
    public bool TrySave(string path, string content)
    {
        if (!IsContentChanged(path, content))
            return false;

        try
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                Logger.Info($"Directory '{directory}' created.");
            }

            var bytesBefore = Exists(path) ? GetFile(path).Length : 0;

            File.WriteAllText(path, content, new UTF8Encoding(false));

            var bytesAfter = GetFile(path).Length;

            Logger.Info($"File \"{Path.GetFileName(path)}\" updated. Size changed from {bytesBefore} to {bytesAfter} bytes.");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to save file '{path}' {ex.Message}");
            return false;
        }
    }

    public bool TryRead(string path, out string content)
    {
        content = string.Empty;

        if (string.IsNullOrWhiteSpace(path))
            return false;

        if (!Exists(path))
            return false;

        try
        {
            content = File.ReadAllText(path, Encoding.UTF8);
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed read file '{path}' {ex.Message}");
            return false;
        }
    }

    public bool IsContentChanged(string path, string currentContent)
    {
        if (!Exists(path))
            return true;

        if (GetFile(path).Length != Encoding.UTF8.GetByteCount(currentContent))
            return true;

        TryRead(path, out var readResult);

        return CalculateHash(readResult) != CalculateHash(currentContent);
    }

    public string CalculateHash(string content)
    {
        content = content.Replace("\r\n", "\n");
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content)));
    }

    public bool Copy(string from, string to)
    {
        try
        {
            if (!Exists(from))
                return false;

            var directory = Path.GetDirectoryName(to);
            if (!string.IsNullOrEmpty(directory))
                CreateDirectories(directory);

            File.Copy(from, to, overwrite: true);
            return true;
        }
        catch (Exception ex)
        {
            throw new IOException($"Failed to copy file from '{from}' to '{to}'.", ex);
        }
    }

    public bool Exists(string path)
    {
        if (string.IsNullOrEmpty(path)) return false;

        return File.Exists(path);
    }

    public void Delete(params string[] paths)
    {
        foreach (var path in paths)
        {
            if (!File.Exists(path))
            {
                Logger.Warn($"File '{path}' not found.");
                continue;
            }

            File.Delete(path);
            Logger.Info($"File '{path}' deleted.");
        }
    }

    public void CreateDirectories(params string[] paths)
    {
        foreach (var path in paths)
        {
            if (Directory.Exists(path))
            {
                Logger.Info($"Directory '{path}' already exists.");
                continue;
            }

            Directory.CreateDirectory(path);
            Logger.Info($"Directory '{path}' created.");
        }
    }

    private FileInfo GetFile(string path)
    {
        return new FileInfo(path);
    }
}