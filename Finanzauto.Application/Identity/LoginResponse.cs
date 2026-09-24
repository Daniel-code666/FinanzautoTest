namespace Finanzauto.Application.Identity;

public class LoginResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string TokenType { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public UserResponse User { get; set; } = null!;
}
