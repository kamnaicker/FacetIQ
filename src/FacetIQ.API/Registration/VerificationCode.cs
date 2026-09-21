using System.Security.Cryptography;
using System.Text;

namespace FacetIQ.API.Registration;

/// <summary>
/// Codes are stored hashed so they are not readable at rest. Six digits can still be brute forced
/// offline, so the expiry and the attempt limit are what protect them.
/// </summary>
public static class VerificationCode
{
    public static string Generate()
    {
        // Exclusive upper bound, so 000000 through 999999, left padded.
        var value = RandomNumberGenerator.GetInt32(0, 1_000_000);

        return value.ToString("D6");
    }

    public static string GenerateCancellationToken()
    {
        // Base64url, since the token travels in a query string.
        var bytes = RandomNumberGenerator.GetBytes(16);
        var base64 = Convert.ToBase64String(bytes);

        return base64.Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }

    public static string Hash(string value)
    {
        return Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    }

    // Fixed time, so a caller cannot learn a prefix from how long a comparison took.
    public static bool Matches(string value, string hash)
    {
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(Hash(value)),
            Encoding.UTF8.GetBytes(hash));
    }
}
