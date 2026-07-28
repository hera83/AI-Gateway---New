using System.Security.Cryptography;
using System.Text;

namespace AiGateway.Data;

// Shared by KeyService (hashing on create/rollover) and ApiKeyAuthenticationHandler (hashing on lookup) —
// keep in one place so the two can never drift and silently break authentication.
public static class ApiKeyHasher
{
    public static string Hash(string plainTextKey) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(plainTextKey)));
}
