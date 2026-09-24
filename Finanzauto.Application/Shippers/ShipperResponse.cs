namespace Finanzauto.Application.Shippers;

public sealed class ShipperResponse
{
    public int Id { get; init; }
    public string CompanyName { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public DateTime CreationDate { get; init; }
    public DateTime? UpdatedDate { get; init; }
}
