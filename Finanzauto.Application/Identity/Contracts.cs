using System.ComponentModel.DataAnnotations;

namespace Finanzauto.Application.Identity;

public sealed class LoginRequest
{
    [Required, EmailAddress, StringLength(254)] public string Email { get; set; } = string.Empty;
    [Required, StringLength(128)] public string Password { get; set; } = string.Empty;
}

public class RegisterRequest
{
    [Required, StringLength(100)] public string FirstName { get; set; } = string.Empty;
    [Required, StringLength(100)] public string LastName { get; set; } = string.Empty;
    [Required, EmailAddress, StringLength(254)] public string Email { get; set; } = string.Empty;
    [Required, StringLength(128, MinimumLength = 12)] public string Password { get; set; } = string.Empty;
}

public sealed class CreateUserRequest : RegisterRequest
{
    [Range(1, int.MaxValue)] public int RoleId { get; set; }
}

public sealed class UpdateUserRequest
{
    [Required, StringLength(100)] public string FirstName { get; set; } = string.Empty;
    [Required, StringLength(100)] public string LastName { get; set; } = string.Empty;
    [Required, EmailAddress, StringLength(254)] public string Email { get; set; } = string.Empty;
    [Range(1, int.MaxValue)] public int RoleId { get; set; }
}

public sealed class ResetPasswordRequest
{
    [Required, StringLength(128, MinimumLength = 12)] public string Password { get; set; } = string.Empty;
    [Required, EmailAddress, StringLength(254)] public string Email { get; set; } = string.Empty;
}

public sealed class RoleRequest
{
    [Required, StringLength(50)] public string Name { get; set; } = string.Empty;
}

public class PageQuery
{
    [Range(1, 1_000_000)] public int Page { get; set; } = 1;
    [Range(1, 100)] public int PageSize { get; set; } = 20;
    [StringLength(100)] public string? Search { get; set; }
    public bool? Active { get; set; } = true;
}

public sealed class UserQuery : PageQuery
{
    [Range(1, int.MaxValue)] public int? RoleId { get; set; }
}

public sealed record PageResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize);
public sealed record UserResponse(int Id, string FirstName, string LastName, string Email,
    int RoleId, string RoleName, bool Active);
public sealed record RoleResponse(int Id, string Name, bool Active);
public sealed record LoginResponse(string AccessToken, string TokenType, DateTime ExpiresAtUtc, UserResponse User);
public sealed record IssuedToken(string Value, DateTime ExpiresAtUtc);
public sealed record SessionUser(int Id, string RoleName);
