using Finanzauto.Application.Identity;
using Finanzauto.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Finanzauto.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public sealed class UserAdministrationController(IUserAdministrationService administration) : ControllerBase
{
    private int ActorId => int.Parse(User.FindFirst("sub")!.Value, System.Globalization.CultureInfo.InvariantCulture);

    [HttpPost("Register")]
    [AllowAnonymous]
    [EnableRateLimiting("PublicIdentity")]
    public async Task<ActionResult<UserResponse>> Register(RegisterRequest request, CancellationToken ct)
        => Ok(await administration.RegisterAsync(request, ct));

    [HttpGet("Users")]
    public async Task<ActionResult<PageResult<UserResponse>>> ListUsers([FromQuery] UserQuery query, CancellationToken ct)
        => Ok(await administration.ListUsersAsync(query, ct));

    [HttpGet("Users/{id:int}")]
    public async Task<ActionResult<UserResponse>> GetUser(int id, CancellationToken ct)
        => Ok(await administration.GetUserAsync(id, ct));

    [HttpPost("Users")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<UserResponse>> CreateUser(CreateUserRequest request, CancellationToken ct)
        => Ok(await administration.CreateAsync(request, ct));

    [HttpPut("Users/{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<UserResponse>> UpdateUser(int id, UpdateUserRequest request, CancellationToken ct)
        => Ok(await administration.UpdateAsync(id, request, ActorId, ct));

    [HttpDelete("Users/{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> DeactivateUser(int id, CancellationToken ct)
    {
        await administration.SetUserActiveAsync(id, false, ActorId, ct);
        return NoContent();
    }

    [HttpPost("Users/{id:int}/Reactivate")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> ReactivateUser(int id, CancellationToken ct)
    {
        await administration.SetUserActiveAsync(id, true, ActorId, ct);
        return NoContent();
    }

    [HttpPut("Users/ResetPassword")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword(int? id, ResetPasswordRequest request, CancellationToken ct)
    {
        await administration.ResetPasswordAsync(id, request, ct);
        return NoContent();
    }

    [HttpGet("Roles")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<PageResult<RoleResponse>>> ListRoles([FromQuery] PageQuery query, CancellationToken ct)
        => await administration.ListRolesAsync(query, ct);

    [HttpGet("Roles/{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<RoleResponse>> GetRole(int id, CancellationToken ct)
        => await administration.GetRoleAsync(id, ct);

    [HttpPost("Roles")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<RoleResponse>> CreateRole(RoleRequest request, CancellationToken ct)
        => Ok(await administration.CreateRoleAsync(request, ct));

    [HttpPut("Roles/{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<RoleResponse>> UpdateRole(int id, RoleRequest request, CancellationToken ct)
        => Ok(await administration.UpdateRoleAsync(id, request, ct));

    [HttpDelete("Roles/{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> DeactivateRole(int id, CancellationToken ct)
    {
        await administration.SetRoleActiveAsync(id, false, ct);
        return NoContent();
    }

    [HttpPost("Roles/{id:int}/Reactivate")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> ReactivateRole(int id, CancellationToken ct)
    {
        await administration.SetRoleActiveAsync(id, true, ct);
        return NoContent();
    }
}
