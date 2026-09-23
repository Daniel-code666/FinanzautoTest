namespace Finanzauto.Authentication;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public string Issuer { get; set; } = "Finanzauto.Api";
    public string Audience { get; set; } = "Finanzauto.Client";
    public string SigningKey { get; set; } = string.Empty;
    public int ExpirationMinutes { get; set; } = 60;
}

