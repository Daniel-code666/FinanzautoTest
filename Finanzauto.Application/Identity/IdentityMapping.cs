using Finanzauto.Domain.Entities;

namespace Finanzauto.Application.Identity;

public static class IdentityMapping
{
    public static UserResponse ToResponse(this Employee user) => new(
        user.EmployeeId, user.FirstName, user.LastName, user.Email,
        user.RoleId, user.Role.Name, user.Active);

    public static RoleResponse ToResponse(this Role role) => new(role.RoleId, role.Name, role.Active);
}

