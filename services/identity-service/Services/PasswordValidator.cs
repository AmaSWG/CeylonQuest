using System.Text.RegularExpressions;
using IdentityService.DTOs;

namespace IdentityService.Services;

public class PasswordValidator
{
    private static readonly Regex PasswordRegex = new(
        RegisterRequest.PasswordPattern,
        RegexOptions.Compiled);

    public static bool IsValid(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            return false;

        return PasswordRegex.IsMatch(password);
    }

    public static string GetRequirementsMessage()
    {
        return RegisterRequest.PasswordRequirementsMessage;
    }
}
