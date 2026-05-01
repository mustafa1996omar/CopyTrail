using System.Security.Cryptography;
using System.Text;

namespace CopyTrail.Utilities;

public static class Hashing
{
    public static string Sha256OfText(string text)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(text);
        return Sha256OfBytes(bytes);
    }

    public static string Sha256OfBytes(byte[] bytes)
    {
        byte[] hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
