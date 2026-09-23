namespace Finanzauto.Domain.Entities;

public sealed class Product : Entity
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int SupplierId { get; set; }
    public int CategoryId { get; set; }
    public string? QuantityPerUnit { get; set; }
    public decimal UnitPrice { get; set; }
    public int UnitsInStock { get; set; }
    public int UnitsOnOrder { get; set; }
    public int ReorderLevel { get; set; }
    public bool Discontinued { get; set; }
    public Supplier Supplier { get; set; } = null!;
    public Category Category { get; set; } = null!;
    public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
}

