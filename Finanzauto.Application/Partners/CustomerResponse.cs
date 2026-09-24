using Finanzauto.Application.Catalog;

namespace Finanzauto.Application.Partners;

public sealed class CustomerResponse : ContactResponse
{
    public string Id { get; init; } = string.Empty;
}
