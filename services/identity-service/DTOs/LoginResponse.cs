namespace IdentityService.DTOs;

public class LoginResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string TokenType { get; set; } = "Bearer";
    public string Role { get; set; } = string.Empty;
}
