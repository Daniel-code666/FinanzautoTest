using System.ComponentModel.DataAnnotations;

namespace Finanzauto.Application.Shippers;

public sealed class ShipperRequest
{
    [Required, StringLength(200)]
    public string CompanyName { get; set; } = string.Empty;

    [StringLength(30)]
    public string? Phone { get; set; }
}
