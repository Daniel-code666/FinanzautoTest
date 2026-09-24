using Finanzauto.Application.Catalog;

namespace Finanzauto.Application.Partners;

public class ContactResponse
{
    public string CompanyName { get; init; } = string.Empty;
    public string? ContactName { get; init; }
    public string? ContactTitle { get; init; }
    public string? Address { get; init; }
    public string? City { get; init; }
    public string? Region { get; init; }
    public string? PostalCode { get; init; }
    public string? Country { get; init; }
    public string? Phone { get; init; }
    public string? Fax { get; init; }
    public DateTime CreationDate { get; init; }
    public DateTime? UpdatedDate { get; init; }
}
