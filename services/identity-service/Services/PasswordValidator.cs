using System.Text.RegularExpressions;
using IdentityService.DTOs;

namespace IdentityService.Services;

/// <summary>
/// Centralizes password validation logic to ensure consistency across registration and password reset.
/// </summary>
public class PasswordValidator
{
    private static readonly Regex PasswordRegex = new(
        RegisterRequest.PasswordPattern,
        RegexOptions.Compiled);

    /// <summary>
    /// Validates that a password meets the project's requirements.
    /// </summary>
    /// <param name="password">The password to validate.</param>
    /// <returns>True if valid, false otherwise.</returns>
    public static bool IsValid(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            return false;

        return PasswordRegex.IsMatch(password);
    }

    /// <summary>
    /// Returns the human-readable password requirements message.
    /// </summary>
    public static string GetRequirementsMessage()
    {
        return RegisterRequest.PasswordRequirementsMessage;
    }
}
