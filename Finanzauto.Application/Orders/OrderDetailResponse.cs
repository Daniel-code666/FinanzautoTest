namespace Finanzauto.Application.Orders;

public sealed class OrderDetailResponse
{
    public int OrderDetailId { get; init; }
    public int ProductId { get; init; }
    public decimal UnitPrice { get; init; }
    public int Quantity { get; init; }
    public decimal Discount { get; init; }
    public decimal Total { get; init; }
    public DateTime CreationDate { get; init; }
    public DateTime? UpdatedDate { get; init; }
}
