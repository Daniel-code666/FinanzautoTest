using System.Text;
using Finanzauto.Application.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Finanzauto.Authentication;

public static class AuthenticationRegistration
{
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var settings = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
        ValidateSettings(settings);

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
        {
            // Se conservan los nombres sub y role usados por los controladores y políticas.
            options.MapInboundClaims = false;
            options.TokenValidationParameters = CreateValidationParameters(settings);
            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = ValidateSessionAsync
            };
        });
        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser().Build())
            .AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
        return services;
    }

    private static void ValidateSettings(JwtOptions settings)
    {
        if (Encoding.UTF8.GetByteCount(settings.SigningKey) < 32 || string.IsNullOrWhiteSpace(settings.Issuer)
            || string.IsNullOrWhiteSpace(settings.Audience) || settings.ExpirationMinutes < 1 || settings.ExpirationMinutes > 1440)
        {
            throw new InvalidOperationException("Configure Jwt:SigningKey (mínimo 32 bytes), Issuer, Audience y ExpirationMinutes (1-1440).");
        }
    }

    private static TokenValidationParameters CreateValidationParameters(JwtOptions settings)
    {
        // Primero se verifican emisor, destinatario, vigencia y firma del token.
        return new TokenValidationParameters
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
    }

    private static async Task ValidateSessionAsync(TokenValidatedContext context)
    {
        // Una firma válida no garantiza que el empleado y su rol sigan activos.
        // GetSessionAsync consulta ese estado actual en la base de datos.
        var principal = context.Principal;
        var employeeIdClaim = principal?.FindFirst("sub")?.Value;
        if (!int.TryParse(employeeIdClaim, out var employeeId))
        {
            context.Fail("Token inválido.");
            return;
        }

        var store = context.HttpContext.RequestServices.GetRequiredService<IIdentityStore>();
        var currentSession = await store.GetSessionAsync(employeeId, context.HttpContext.RequestAborted);
        var tokenRole = principal!.FindFirst("role")?.Value;
        if (currentSession == null || tokenRole != currentSession.RoleName)
        {
            context.Fail("La sesión ya no es válida.");
        }
    }
}
