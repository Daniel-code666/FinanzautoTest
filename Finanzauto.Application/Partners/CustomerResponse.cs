using Finanzauto.Application.Catalog;

namespace Finanzauto.Application.Partners;

public sealed class CustomerResponse : ContactResponse
{
    public int Id { get; init; }
}
