using System.ComponentModel.DataAnnotations;

namespace Finanzauto.Application.Identity;

public sealed class ResetPasswordRequest
{
    [Required, StringLength(128, MinimumLength = 12)]
    public string Password { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(254)]
    public string Email { get; set; } = string.Empty;
}
