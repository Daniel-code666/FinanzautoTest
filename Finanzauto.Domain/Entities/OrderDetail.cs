namespace Finanzauto.Domain.Entities;

public sealed class OrderDetail : Entity
{
    public int OrderDetailId { get; set; }
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal Discount { get; set; }
    public Order Order { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
