using Finanzauto.Domain.Entities;

namespace Finanzauto.Application.Identity;

public sealed class ProfileService(IIdentityStore store, IPasswordService passwords) : IProfileService
{
    public async Task<ProfileResponse> GetAsync(int userId, CancellationToken ct)
    {
        var user = await RequireUserAsync(userId, ct);
        return Map(user);
    }

    public async Task<ProfileResponse> UpdateAsync(int userId, UpdateProfileRequest request, CancellationToken ct)
    {
        var user = await RequireUserAsync(userId, ct);
        var email = request.Email.Trim();
        if (await store.EmailExistsAsync(email.ToUpperInvariant(), userId, ct))
        {
            throw new IdentityException(409, "El correo ya está registrado.");
        }

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.Email = email;
        user.BirthDate = request.BirthDate;
        user.Address = request.Address?.Trim();
        user.City = request.City?.Trim();
        user.Region = request.Region?.Trim();
        user.PostalCode = request.PostalCode?.Trim();
        user.Country = request.Country?.Trim();
        user.HomePhone = request.HomePhone?.Trim();
        await store.SaveAsync(ct);
        return Map(user);
    }

    public async Task ChangePasswordAsync(int userId, ChangeProfilePasswordRequest request, CancellationToken ct)
    {
        var user = await RequireUserAsync(userId, ct);
        if (!passwords.Verify(user, request.CurrentPassword))
        {
            throw new IdentityException(400, "La contraseña actual no es correcta.");
        }

        user.PasswordHash = passwords.Hash(user, request.NewPassword);
        await store.SaveAsync(ct);
    }

    private async Task<Employee> RequireUserAsync(int userId, CancellationToken ct)
    {
        var user = await store.FindUserAsync(userId, ct);
        if (user is null || !user.Active || !user.Role.Active)
        {
            throw new IdentityException(401, "La sesión ya no es válida.");
        }

        return user;
    }

    private static ProfileResponse Map(Employee user)
    {
        return new ProfileResponse
        {
            Id = user.EmployeeId,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            BirthDate = user.BirthDate,
            Address = user.Address,
            City = user.City,
            Region = user.Region,
            PostalCode = user.PostalCode,
            Country = user.Country,
            HomePhone = user.HomePhone,
            RoleId = user.RoleId,
            RoleName = user.Role.Name,
            CreationDate = user.CreationDate,
            UpdatedDate = user.UpdatedDate
        };
    }
}
