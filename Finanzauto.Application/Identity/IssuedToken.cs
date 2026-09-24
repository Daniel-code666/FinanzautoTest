namespace Finanzauto.Application.Identity;

public class IssuedToken
{
    public string Value { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
}
