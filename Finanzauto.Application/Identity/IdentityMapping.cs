using Finanzauto.Domain.Entities;

namespace Finanzauto.Application.Identity;

public static class IdentityMapping
{
    public static UserResponse ToResponse(this Employee user)
    {
        return new UserResponse
        {
            Id = user.EmployeeId,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            RoleId = user.RoleId,
            RoleName = user.Role.Name,
            Active = user.Active
        };
    }

    public static RoleResponse ToResponse(this Role role)
    {
        return new RoleResponse
        {
            Id = role.RoleId,
            Name = role.Name,
            Active = role.Active
        };
    }
}
