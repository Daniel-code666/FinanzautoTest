using System.ComponentModel.DataAnnotations;
using Finanzauto.Application.Catalog;

namespace Finanzauto.Application.Partners;

public class ContactRequest
{
    [Required, StringLength(200)] public string CompanyName { get; set; } = string.Empty;
    [StringLength(100)] public string? ContactName { get; set; }
    [StringLength(100)] public string? ContactTitle { get; set; }
    [StringLength(300)] public string? Address { get; set; }
    [StringLength(100)] public string? City { get; set; }
    [StringLength(100)] public string? Region { get; set; }
    [StringLength(20)] public string? PostalCode { get; set; }
    [StringLength(100)] public string? Country { get; set; }
    [StringLength(30)] public string? Phone { get; set; }
    [StringLength(30)] public string? Fax { get; set; }
}
public sealed class SupplierRequest : ContactRequest
{
    [StringLength(2048)] public string? HomePage { get; set; }
}
public sealed class CustomerRequest : ContactRequest
{
    [Required, RegularExpression("^[A-Za-z0-9]{1,5}$")]
    public string CustomerId { get; set; } = string.Empty;
}
public sealed class PartnerQuery : CatalogQuery
{
    [StringLength(100)] public string? Country { get; set; }
    [StringLength(100)] public string? City { get; set; }
}
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
public sealed class SupplierDetailResponse : ContactResponse
{
    public int Id { get; init; }
    public string? HomePage { get; init; }
}
public sealed class CustomerResponse : ContactResponse
{
    public string Id { get; init; } = string.Empty;
}
public sealed record BulkResponse<T>(int CreatedCount, IReadOnlyList<T> Items);

