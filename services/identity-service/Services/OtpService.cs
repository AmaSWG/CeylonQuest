using System.Security.Cryptography;

namespace IdentityService.Services;

public class OtpService
{
    public const int DefaultExpiryMinutes = 15;

    public string GenerateCode()
    {
        var value = RandomNumberGenerator.GetInt32(0, 1_000_000);
        return value.ToString("D6");
    }
}
