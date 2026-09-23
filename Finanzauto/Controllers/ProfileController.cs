using System.Globalization;
using Finanzauto.Application.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Finanzauto.Controllers;

[ApiController]
[Route("Profile")]
[Authorize]
public sealed class ProfileController(IProfileService profile) : ControllerBase
{
    private int UserId => int.Parse(User.FindFirst("sub")!.Value, CultureInfo.InvariantCulture);

    [HttpGet]
    public async Task<ActionResult<ProfileResponse>> Get(CancellationToken ct) =>
        Ok(await profile.GetAsync(UserId, ct));

    [HttpPut]
    public async Task<ActionResult<ProfileResponse>> Update(UpdateProfileRequest request, CancellationToken ct) =>
        Ok(await profile.UpdateAsync(UserId, request, ct));

    [HttpPut("Password")]
    public async Task<IActionResult> ChangePassword(ChangeProfilePasswordRequest request, CancellationToken ct)
    {
        await profile.ChangePasswordAsync(UserId, request, ct);
        return NoContent();
    }
}
