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
    {
        var result = await administration.RegisterAsync(request, ct);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpGet("Users")]
    public async Task<ActionResult<PageResult<UserResponse>>> ListUsers([FromQuery] UserQuery query, CancellationToken ct)
    {
        var result = await administration.ListUsersAsync(query, ct);
        return Ok(result);
    }

    [HttpGet("Users/{id:int}")]
    public async Task<ActionResult<UserResponse>> GetUser(int id, CancellationToken ct)
    {
        var result = await administration.GetUserAsync(id, ct);
        return Ok(result);
    }

    [HttpPost("Users")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<UserResponse>> CreateUser(CreateUserRequest request, CancellationToken ct)
    {
        var user = await administration.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
    }

    [HttpPut("Users/{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<UserResponse>> UpdateUser(int id, UpdateUserRequest request, CancellationToken ct)
    {
        var result = await administration.UpdateAsync(id, request, ActorId, ct);
        return Ok(result);
    }

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
    {
        var result = await administration.ListRolesAsync(query, ct);
        return Ok(result);
    }

    [HttpGet("Roles/{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<RoleResponse>> GetRole(int id, CancellationToken ct)
    {
        var result = await administration.GetRoleAsync(id, ct);
        return Ok(result);
    }

    [HttpPost("Roles")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<RoleResponse>> CreateRole(RoleRequest request, CancellationToken ct)
    {
        var role = await administration.CreateRoleAsync(request, ct);
        return CreatedAtAction(nameof(GetRole), new { id = role.Id }, role);
    }

    [HttpPut("Roles/{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<RoleResponse>> UpdateRole(int id, RoleRequest request, CancellationToken ct)
    {
        var result = await administration.UpdateRoleAsync(id, request, ct);
        return Ok(result);
    }

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
