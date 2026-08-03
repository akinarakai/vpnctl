using System.Security.Cryptography;
using System.Text;

public static class TokenHasher
{
    public static string Hash(string token)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }

    public static bool Verify(string token, string hash)
    {
        return hash == Hash(token);
    }
}