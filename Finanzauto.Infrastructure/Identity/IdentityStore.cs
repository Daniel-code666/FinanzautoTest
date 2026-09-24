using Finanzauto.Application.Identity;
using Finanzauto.Domain.Entities;
using Finanzauto.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Finanzauto.Infrastructure.Identity;

public sealed class IdentityStore(FinanzautoDbContext db) : IIdentityStore
{
    public Task<Employee?> FindByEmailAsync(string normalizedEmail, CancellationToken ct) =>
        db.Employees.IgnoreQueryFilters().Include(x => x.Role)
            .SingleOrDefaultAsync(x => x.NormalizedEmail == normalizedEmail, ct);

    public Task<Employee?> FindUserAsync(int id, CancellationToken ct) =>
        db.Employees.IgnoreQueryFilters().Include(x => x.Role)
            .SingleOrDefaultAsync(x => x.EmployeeId == id, ct);

    public Task<Role?> FindRoleAsync(int id, CancellationToken ct) =>
        db.Roles.IgnoreQueryFilters().SingleOrDefaultAsync(x => x.RoleId == id, ct);

    public Task<bool> EmailExistsAsync(string normalizedEmail, int? exceptId, CancellationToken ct) =>
        db.Employees.IgnoreQueryFilters().AnyAsync(
            x => x.NormalizedEmail == normalizedEmail && (!exceptId.HasValue || x.EmployeeId != exceptId), ct);

    public Task<bool> RoleNameExistsAsync(string normalizedName, int? exceptId, CancellationToken ct) =>
        db.Roles.IgnoreQueryFilters().AnyAsync(
            x => x.NormalizedName == normalizedName && (!exceptId.HasValue || x.RoleId != exceptId), ct);

    public async Task<PageResult<UserResponse>> ListUsersAsync(UserQuery query, CancellationToken ct)
    {
        var users = db.Employees.IgnoreQueryFilters().AsNoTracking();
        if (query.Active.HasValue) users = users.Where(x => x.Active == query.Active.Value);
        if (query.RoleId.HasValue) users = users.Where(x => x.RoleId == query.RoleId.Value);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToUpperInvariant();
            users = users.Where(x => x.NormalizedEmail.Contains(term) ||
                x.FirstName.ToUpper().Contains(term) || x.LastName.ToUpper().Contains(term));
        }
        var total = await users.CountAsync(ct);
        var items = await users.OrderBy(x => x.EmployeeId)
            .Skip((query.Page - 1) * query.PageSize).Take(query.PageSize)
            .Select(x => new UserResponse
            {
                Id = x.EmployeeId,
                FirstName = x.FirstName,
                LastName = x.LastName,
                Email = x.Email,
                RoleId = x.RoleId,
                RoleName = x.Role.Name,
                Active = x.Active
            }).ToListAsync(ct);
        return new PageResult<UserResponse>
        {
            Items = items,
            TotalCount = total,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }

    public async Task<PageResult<RoleResponse>> ListRolesAsync(PageQuery query, CancellationToken ct)
    {
        var roles = db.Roles.IgnoreQueryFilters().AsNoTracking();
        if (query.Active.HasValue) roles = roles.Where(x => x.Active == query.Active.Value);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToUpperInvariant();
            roles = roles.Where(x => x.NormalizedName.Contains(term));
        }
        var total = await roles.CountAsync(ct);
        var items = await roles.OrderBy(x => x.RoleId)
            .Skip((query.Page - 1) * query.PageSize).Take(query.PageSize)
            .Select(x => new RoleResponse
            {
                Id = x.RoleId,
                Name = x.Name,
                Active = x.Active
            }).ToListAsync(ct);
        return new PageResult<RoleResponse>
        {
            Items = items,
            TotalCount = total,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }

    public Task<SessionUser?> GetSessionAsync(int id, CancellationToken ct) =>
        db.Employees.IgnoreQueryFilters().AsNoTracking()
            .Where(x => x.EmployeeId == id && x.Active && x.Role.Active)
            .Select(x => new SessionUser
            {
                Id = x.EmployeeId,
                RoleName = x.Role.Name
            })
            .SingleOrDefaultAsync(ct);

    public void AddUser(Employee employee) => db.Employees.Add(employee);
    public void AddRole(Role role) => db.Roles.Add(role);

    public async Task SaveAsync(CancellationToken ct)
    {
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
        { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new IdentityException(409, "Ya existe un registro con ese correo o nombre de rol.");
        }
    }
}
