using System.Security.Cryptography;

namespace IdentityService.Services;

// Reusable one-time password generator for account activation flows.
public class OtpService
{
    public const int DefaultExpiryMinutes = 15;

    public string GenerateCode()
    {
        // 6-digit numeric OTP, zero-padded.
        var value = RandomNumberGenerator.GetInt32(0, 1_000_000);
        return value.ToString("D6");
    }
}
