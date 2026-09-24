using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Finanzauto.Application.Identity;
using Finanzauto.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Finanzauto.Authentication;

public sealed class JwtTokenService(IOptions<JwtOptions> options, TimeProvider timeProvider) : ITokenService
{
    public IssuedToken Issue(Employee employee)
    {
        var settings = options.Value;
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var expires = now.AddMinutes(settings.ExpirationMinutes);
        // sub identifica al empleado, jti al token y role contiene el nombre del rol.
        Claim[] claims =
        [
            new(JwtRegisteredClaimNames.Sub, employee.EmployeeId.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("role", employee.Role.Name)
        ];
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SigningKey));
        var signingCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        // La firma permite detectar alteraciones; el contenido del JWT no está cifrado.
        var token = new JwtSecurityToken(settings.Issuer, settings.Audience, claims, notBefore: now, expires: expires, signingCredentials: signingCredentials);
        return new IssuedToken
        {
            Value = new JwtSecurityTokenHandler().WriteToken(token),
            ExpiresAtUtc = expires
        };
    }
}
