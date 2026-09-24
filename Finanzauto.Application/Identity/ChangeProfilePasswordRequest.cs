using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Finanzauto.Application.Identity;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class ChangeProfilePasswordRequest
{
    [Required, StringLength(128)]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required, StringLength(128, MinimumLength = 12)]
    public string NewPassword { get; set; } = string.Empty;
}
