using System.ComponentModel.DataAnnotations;
using Finanzauto.Application.Catalog;

namespace Finanzauto.Application.Partners;

public sealed class SupplierRequest : ContactRequest
{
    [StringLength(2048)]
    public string? HomePage { get; set; }
}
