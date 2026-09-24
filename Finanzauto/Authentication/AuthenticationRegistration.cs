using System.Text;
using Finanzauto.Application.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Finanzauto.Authentication;

public static class AuthenticationRegistration
{
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var settings = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new();
        if (Encoding.UTF8.GetByteCount(settings.SigningKey) < 32 || string.IsNullOrWhiteSpace(settings.Issuer)
            || string.IsNullOrWhiteSpace(settings.Audience) || settings.ExpirationMinutes is < 1 or > 1440)
            throw new InvalidOperationException("Configure Jwt:SigningKey (mínimo 32 bytes), Issuer, Audience y ExpirationMinutes (1-1440).");

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
        {
            options.MapInboundClaims = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = settings.Issuer,
                ValidateAudience = true,
                ValidAudience = settings.Audience,
                ValidateLifetime = true,
                RequireExpirationTime = true,
                RequireSignedTokens = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SigningKey)),
                ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                ClockSkew = TimeSpan.FromSeconds(30),
                NameClaimType = "sub",
                RoleClaimType = "role"
            };
            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = async context =>
                {
                    var principal = context.Principal;
                    if (!int.TryParse(principal?.FindFirst("sub")?.Value, out var id))
                    {
                        context.Fail("Token inválido.");
                        return;
                    }
                    var store = context.HttpContext.RequestServices.GetRequiredService<IIdentityStore>();
                    var user = await store.GetSessionAsync(id, context.HttpContext.RequestAborted);
                    if (user is null || principal!.FindFirst("role")?.Value != user.RoleName)
                        context.Fail("La sesión ya no es válida.");
                }
            };
        });
        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser().Build())
            .AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
        return services;
    }
}
