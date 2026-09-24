using System.ComponentModel.DataAnnotations;
using Finanzauto.Application.Catalog;

namespace Finanzauto.Application.Partners;

public sealed class PartnerQuery : CatalogQuery
{
    [StringLength(100)]
    public string? Country { get; set; }

    [StringLength(100)]
    public string? City { get; set; }
}
