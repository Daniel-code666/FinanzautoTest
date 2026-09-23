using Finanzauto.Domain.Entities;

namespace Finanzauto.Application.Identity;

public interface IIdentityStore
{
    Task<Employee?> FindByEmailAsync(string normalizedEmail, CancellationToken ct);
    Task<Employee?> FindUserAsync(int id, CancellationToken ct);
    Task<Role?> FindRoleAsync(int id, CancellationToken ct);
    Task<bool> EmailExistsAsync(string normalizedEmail, int? exceptId, CancellationToken ct);
    Task<bool> RoleNameExistsAsync(string normalizedName, int? exceptId, CancellationToken ct);
    Task<PageResult<UserResponse>> ListUsersAsync(UserQuery query, CancellationToken ct);
    Task<PageResult<RoleResponse>> ListRolesAsync(PageQuery query, CancellationToken ct);
    Task<SessionUser?> GetSessionAsync(int id, CancellationToken ct);
    void AddUser(Employee employee);
    void AddRole(Role role);
    Task SaveAsync(CancellationToken ct);
}

public interface IPasswordService
{
    string Hash(Employee employee, string password);
    bool Verify(Employee employee, string password);
}

public interface ITokenService
{
    IssuedToken Issue(Employee employee);
}

public interface ILoginService
{
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct);
}

public interface IUserAdministrationService
{
    Task<UserResponse> RegisterAsync(RegisterRequest request, CancellationToken ct);
    Task<UserResponse> CreateAsync(CreateUserRequest request, CancellationToken ct);
    Task<PageResult<UserResponse>> ListUsersAsync(UserQuery query, CancellationToken ct);
    Task<UserResponse> GetUserAsync(int id, CancellationToken ct);
    Task<UserResponse> UpdateAsync(int id, UpdateUserRequest request, int actorId, CancellationToken ct);
    Task SetUserActiveAsync(int id, bool active, int actorId, CancellationToken ct);
    Task ResetPasswordAsync(int? id, ResetPasswordRequest request, CancellationToken ct);
    Task<PageResult<RoleResponse>> ListRolesAsync(PageQuery query, CancellationToken ct);
    Task<RoleResponse> GetRoleAsync(int id, CancellationToken ct);
    Task<RoleResponse> CreateRoleAsync(RoleRequest request, CancellationToken ct);
    Task<RoleResponse> UpdateRoleAsync(int id, RoleRequest request, CancellationToken ct);
    Task SetRoleActiveAsync(int id, bool active, CancellationToken ct);
}

