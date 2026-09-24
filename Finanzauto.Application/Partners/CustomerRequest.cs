using System.ComponentModel.DataAnnotations;
using Finanzauto.Application.Catalog;

namespace Finanzauto.Application.Partners;

public sealed class CustomerRequest : ContactRequest
{
    [Required, RegularExpression("^[A-Za-z0-9]{1,5}$")]
    public string CustomerId { get; set; } = string.Empty;
}
