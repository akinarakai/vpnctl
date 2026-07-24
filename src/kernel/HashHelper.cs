using System.Security.Cryptography;
using System.Text;

public static class HashHelper
{
    public static bool IsChanged(string path, string currentData)
    {
        if (!File.Exists(path))
        {
            return true;
        }

        var driveData = File.ReadAllText(path);
        var driveHash = CalculateHash(driveData);

        var currentHash = CalculateHash(currentData);
        return driveHash != currentHash;
    }

    public static string CalculateHash(string data)
    {
        data = data.Replace("\r\n", "\n");

        var dataBytes = Encoding.UTF8.GetBytes(data);
        var hashBytes = SHA256.HashData(dataBytes);
        return Convert.ToHexString(hashBytes);
    }
}