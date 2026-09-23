namespace Finanzauto.Domain.Entities;

public sealed class Employee : Entity
{
    public int EmployeeId { get; set; }
    public string LastName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? TitleOfCourtesy { get; set; }
    public DateOnly? BirthDate { get; set; }
    public DateOnly? HireDate { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Region { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
    public string? HomePhone { get; set; }
    public string? Extension { get; set; }
    public byte[]? Photo { get; set; }
    public string? Notes { get; set; }
    public int? ReportsTo { get; set; }
    public string Email { get; set; } = string.Empty;
    public string NormalizedEmail { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;
    public Employee? Supervisor { get; set; }
    public ICollection<Employee> Subordinates { get; set; } = new List<Employee>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
