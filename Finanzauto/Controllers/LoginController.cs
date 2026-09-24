using Finanzauto.Application.Identity;
using Finanzauto.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Finanzauto.Controllers;

[ApiController]
[Route("[controller]")]
public sealed class LoginController(ILoginService login) : ControllerBase
{
    [HttpPost]
    [AllowAnonymous]
    [EnableRateLimiting("PublicIdentity")]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> Post(LoginRequest request, CancellationToken ct)
    {
        var result = await login.LoginAsync(request, ct);
        return Ok(result);
    }
}
