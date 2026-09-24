using System.ComponentModel.DataAnnotations;
using Finanzauto.Application.Catalog;

namespace Finanzauto.Application.Partners;

public class ContactRequest
{
    [Required, StringLength(200)]
    public string CompanyName { get; set; } = string.Empty;

    [StringLength(100)]
    public string? ContactName { get; set; }

    [StringLength(100)]
    public string? ContactTitle { get; set; }

    [StringLength(300)]
    public string? Address { get; set; }

    [StringLength(100)]
    public string? City { get; set; }

    [StringLength(100)]
    public string? Region { get; set; }

    [StringLength(20)]
    public string? PostalCode { get; set; }

    [StringLength(100)]
    public string? Country { get; set; }

    [StringLength(30)]
    public string? Phone { get; set; }

    [StringLength(30)]
    public string? Fax { get; set; }
}
