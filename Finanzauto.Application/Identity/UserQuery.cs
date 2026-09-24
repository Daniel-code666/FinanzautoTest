using System.ComponentModel.DataAnnotations;

namespace Finanzauto.Application.Identity;

public sealed class UserQuery : PageQuery
{
    [Range(1, int.MaxValue)]
    public int? RoleId { get; set; }
}
