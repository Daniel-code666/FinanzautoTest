namespace Finanzauto.Application.Orders;

public sealed class OrderResponse
{
    public int Id { get; init; }
    public int CustomerId { get; init; }
    public int EmployeeId { get; init; }
    public int? ShipVia { get; init; }
    public DateTime OrderDate { get; init; }
    public DateTime? RequiredDate { get; init; }
    public DateTime? ShippedDate { get; init; }
    public decimal Freight { get; init; }
    public string? ShipName { get; init; }
    public string? ShipAddress { get; init; }
    public string? ShipCity { get; init; }
    public string? ShipRegion { get; init; }
    public string? ShipPostalCode { get; init; }
    public string? ShipCountry { get; init; }
    public DateTime CreationDate { get; init; }
    public DateTime? UpdatedDate { get; init; }
    public IReadOnlyList<OrderDetailResponse> Details { get; init; } = [];
    public decimal Total { get; init; }
}
