public static class FileHelper
{
    public static void TrySaveFile(string path, string data)
    {
        if (!HashHelper.IsChanged(path, data)) return;

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

            File.WriteAllText(path, data);

            fileInfo.Refresh();
            long bytesAfter = fileInfo.Length;

            Logger.Info($"File \"{Path.GetFileName(path)}\" updated. Size changed from {bytesBefore} to {bytesAfter} bytes.");
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to save file at {path}. {ex.Message}", ex);
        }
    }
}