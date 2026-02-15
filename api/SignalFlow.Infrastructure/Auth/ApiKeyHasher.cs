using System.Security.Cryptography;
using System.Text;

namespace SignalFlow.Infrastructure.Auth;

public static class ApiKeyHasher
{
    public static string Sha256Hex(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
