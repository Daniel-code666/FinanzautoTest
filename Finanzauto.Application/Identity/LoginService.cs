namespace Finanzauto.Application.Identity;

public sealed class LoginService(IIdentityStore store, IPasswordService passwords, ITokenService tokens) : ILoginService
{
    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var user = await store.FindByEmailAsync(request.Email.Trim().ToUpperInvariant(), ct);
        if (user is null || !passwords.Verify(user, request.Password) || !user.Active || !user.Role.Active)
            throw new IdentityException(401, "Credenciales inválidas.");

        var token = tokens.Issue(user);
        return new LoginResponse(token.Value, "Bearer", token.ExpiresAtUtc, user.ToResponse());
    }
}

