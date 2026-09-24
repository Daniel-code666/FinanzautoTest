using System.ComponentModel.DataAnnotations;

namespace Finanzauto.Application.Identity;

public sealed class CreateUserRequest : RegisterRequest
{
    [Range(1, int.MaxValue)]
    public int RoleId { get; set; }
}
