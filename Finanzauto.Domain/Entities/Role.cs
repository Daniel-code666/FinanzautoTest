namespace Finanzauto.Domain.Entities;

public sealed class Role : Entity
{
    public int RoleId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NormalizedName { get; private set; } = string.Empty;
    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}
