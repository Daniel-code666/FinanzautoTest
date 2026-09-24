using Finanzauto.Application.Catalog;

namespace Finanzauto.Application.Partners;

public sealed class SupplierDetailResponse : ContactResponse
{
    public int Id { get; init; }
    public string? HomePage { get; init; }
}
