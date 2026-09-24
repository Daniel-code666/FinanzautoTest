using System.ComponentModel.DataAnnotations;

namespace Finanzauto.Application.Identity;

public sealed class UpdateUserRequest
{
    [Required, StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(254)]
    public string Email { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int RoleId { get; set; }
}
