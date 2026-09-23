namespace Finanzauto.Domain.Entities;

public sealed class Shipper : Entity
{
    public int ShipperId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}

