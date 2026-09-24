using Finanzauto.Domain.Entities;

namespace Finanzauto.Application.Identity;

public sealed class UserAdministrationService(IIdentityStore store, IPasswordService passwords) : IUserAdministrationService
{
    public const int AdminRoleId = 1;
    public const int UserRoleId = 2;
    public Task<UserResponse> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        return CreateUserAsync(request, UserRoleId, ct);
    }

    public Task<UserResponse> CreateAsync(CreateUserRequest request, CancellationToken ct)
    {
        return CreateUserAsync(request, request.RoleId, ct);
    }

    private async Task<UserResponse> CreateUserAsync(RegisterRequest request, int roleId, CancellationToken ct)
    {
        await CheckEmailAsync(request.Email, null, ct);
        var role = await RequireActiveRoleAsync(roleId, ct);
        var user = new Employee
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = request.Email.Trim(),
            RoleId = role.RoleId,
            Role = role
        };
        user.PasswordHash = passwords.Hash(user, request.Password);
        store.AddUser(user);
        await store.SaveAsync(ct);
        return user.ToResponse();
    }

    public Task<PageResult<UserResponse>> ListUsersAsync(UserQuery query, CancellationToken ct)
    {
        return store.ListUsersAsync(query, ct);
    }

    public async Task<UserResponse> GetUserAsync(int id, CancellationToken ct)
    {
        var user = await RequireUserAsync(id, ct);
        return user.ToResponse();
    }

    public async Task<UserResponse> UpdateAsync(int id, UpdateUserRequest request, int actorId, CancellationToken ct)
    {
        var user = await RequireUserAsync(id, ct);
        if (id == actorId && request.RoleId != user.RoleId)
        {
            throw new IdentityException(409, "No puedes cambiar tu propio rol.");
        }

        await CheckEmailAsync(request.Email, id, ct);
        var role = await RequireActiveRoleAsync(request.RoleId, ct);
        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.Email = request.Email.Trim();
        user.RoleId = role.RoleId;
        user.Role = role;
        await store.SaveAsync(ct);
        return user.ToResponse();
    }

    public async Task SetUserActiveAsync(int id, bool active, int actorId, CancellationToken ct)
    {
        if (id == actorId && !active)
        {
            throw new IdentityException(409, "No puedes desactivar tu propia cuenta.");
        }

        var user = await RequireUserAsync(id, ct);
        if (active)
        {
            await RequireActiveRoleAsync(user.RoleId, ct);
        }

        user.Active = active;
        await store.SaveAsync(ct);
    }

    public async Task ResetPasswordAsync(int? id, ResetPasswordRequest request, CancellationToken ct)
    {
        Employee user;
        if (id.HasValue)
        {
            user = await RequireUserAsync(id.Value, ct);
        }
        else
        {
            var existingUser = await store.FindByEmailAsync(request.Email.Trim().ToUpperInvariant(), ct);
            if (existingUser == null)
            {
                throw new IdentityException(404, "Usuario no encontrado.");
            }
            user = existingUser;
        }

        user.PasswordHash = passwords.Hash(user, request.Password);
        await store.SaveAsync(ct);
    }

    public Task<PageResult<RoleResponse>> ListRolesAsync(PageQuery query, CancellationToken ct)
    {
        return store.ListRolesAsync(query, ct);
    }

    public async Task<RoleResponse> GetRoleAsync(int id, CancellationToken ct)
    {
        var role = await RequireRoleAsync(id, ct);
        return role.ToResponse();
    }

    public async Task<RoleResponse> CreateRoleAsync(RoleRequest request, CancellationToken ct)
    {
        await CheckRoleNameAsync(request.Name, null, ct);
        var role = new Role
        {
            Name = request.Name.Trim()
        };
        store.AddRole(role);
        await store.SaveAsync(ct);
        return role.ToResponse();
    }

    public async Task<RoleResponse> UpdateRoleAsync(int id, RoleRequest request, CancellationToken ct)
    {
        var role = await RequireRoleAsync(id, ct);
        if (id == AdminRoleId || id == UserRoleId)
        {
            throw new IdentityException(409, "Los nombres Admin y User están reservados.");
        }

        await CheckRoleNameAsync(request.Name, id, ct);
        role.Name = request.Name.Trim();
        await store.SaveAsync(ct);
        return role.ToResponse();
    }

    public async Task SetRoleActiveAsync(int id, bool active, CancellationToken ct)
    {
        var role = await RequireRoleAsync(id, ct);
        if (!active && (id == AdminRoleId || id == UserRoleId))
        {
            throw new IdentityException(409, "Los roles base Admin y User no se pueden desactivar.");
        }

        role.Active = active;
        await store.SaveAsync(ct);
    }

    private async Task<Employee> RequireUserAsync(int id, CancellationToken ct)
    {
        var user = await store.FindUserAsync(id, ct);
        if (user == null)
        {
            throw new IdentityException(404, "Usuario no encontrado.");
        }
        return user;
    }

    private async Task<Role> RequireRoleAsync(int id, CancellationToken ct)
    {
        var role = await store.FindRoleAsync(id, ct);
        if (role == null)
        {
            throw new IdentityException(404, "Rol no encontrado.");
        }
        return role;
    }

    private async Task<Role> RequireActiveRoleAsync(int id, CancellationToken ct)
    {
        var role = await RequireRoleAsync(id, ct);
        if (!role.Active)
        {
            throw new IdentityException(409, "El rol está inactivo.");
        }

        return role;
    }

    private async Task CheckEmailAsync(string email, int? exceptId, CancellationToken ct)
    {
        if (await store.EmailExistsAsync(email.Trim().ToUpperInvariant(), exceptId, ct))
        {
            throw new IdentityException(409, "El correo ya está registrado.");
        }
    }

    private async Task CheckRoleNameAsync(string name, int? exceptId, CancellationToken ct)
    {
        if (await store.RoleNameExistsAsync(name.Trim().ToUpperInvariant(), exceptId, ct))
        {
            throw new IdentityException(409, "El nombre del rol ya existe.");
        }
    }
}
