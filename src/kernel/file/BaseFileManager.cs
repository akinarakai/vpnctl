using System.Security.Cryptography;
using System.Text;

public class BaseFileManager : IFileManager
{
    public bool TrySaveFile(string path, string content)
    {
        if (!IsContentChanged(path, content)) return false;

        try
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                Logger.Info($"Directory {directory} was created.");
            }

            long bytesBefore = 0;
            var fileInfo = new FileInfo(path);
            if (fileInfo.Exists)
            {
                bytesBefore = fileInfo.Length;
            }

            File.WriteAllText(path, content);

            fileInfo.Refresh();
            long bytesAfter = fileInfo.Length;

            Logger.Info($"File \"{Path.GetFileName(path)}\" updated. Size changed from {bytesBefore} to {bytesAfter} bytes.");
            return true;
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to save file at {path}. {ex.Message}", ex);
        }
    }

    public bool IsContentChanged(string path, string currentContent)
    {
        if (!Exists(path)) return true;

        var readResult = Kernel.Cmd.Run("sudo", $"cat {path}", true, false);
        if (!readResult.Success) return true;

        var driveHash = CalculateHash(readResult.Text);
        var currentHash = CalculateHash(currentContent);
        
        return driveHash != currentHash;
    }

    public string CalculateHash(string content)
    {
        if (string.IsNullOrEmpty(content)) return string.Empty;
        
        content = content.Replace("\r\n", "\n");

        var dataBytes = Encoding.UTF8.GetBytes(content);
        var hashBytes = SHA256.HashData(dataBytes);
        return Convert.ToHexString(hashBytes);
    }

    public bool Exists(params string[] paths)
    {
        if (paths == null || paths.Length == 0) return false;

        return paths.Any(path => File.Exists(path));
    }

    public void Delete(params string[] paths)
    {
        if (paths == null || paths.Length == 0) return;

        foreach (var path in paths)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
                Logger.Info($"\"{path}\" was deleted.");
            }
            else
            {
                Logger.Warn($"\"{path}\" not found for delete.");
            }
        }
    }
}