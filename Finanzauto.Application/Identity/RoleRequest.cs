using System.ComponentModel.DataAnnotations;

namespace Finanzauto.Application.Identity;

public sealed class RoleRequest
{
    [Required, StringLength(50)]
    public string Name { get; set; } = string.Empty;
}
