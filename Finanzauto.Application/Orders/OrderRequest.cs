using System.ComponentModel.DataAnnotations;

namespace Finanzauto.Application.Orders;

public sealed class OrderRequest : IValidatableObject
{
    [Range(1, int.MaxValue)]
    public int CustomerId { get; set; }

    [Range(1, int.MaxValue)]
    public int? ShipVia { get; set; }

    public DateTime OrderDate { get; set; }
    public DateTime? RequiredDate { get; set; }
    public DateTime? ShippedDate { get; set; }

    [Range(typeof(decimal), "0", "9999999999999999.99", ParseLimitsInInvariantCulture = true)]
    public decimal Freight { get; set; }

    [StringLength(200)]
    public string? ShipName { get; set; }
    [StringLength(300)]
    public string? ShipAddress { get; set; }
    [StringLength(100)]
    public string? ShipCity { get; set; }
    [StringLength(100)]
    public string? ShipRegion { get; set; }
    [StringLength(20)]
    public string? ShipPostalCode { get; set; }
    [StringLength(100)]
    public string? ShipCountry { get; set; }

    [Required, MinLength(1), MaxLength(1000)]
    public OrderDetailRequest[] Details { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (OrderDate == default || OrderDate.Kind != DateTimeKind.Utc || (RequiredDate.HasValue && RequiredDate.Value.Kind != DateTimeKind.Utc) ||
            (ShippedDate.HasValue && ShippedDate.Value.Kind != DateTimeKind.Utc))
            yield return new ValidationResult("Las fechas deben enviarse en UTC; OrderDate es obligatoria.");

        if (RequiredDate < OrderDate || ShippedDate < OrderDate)
            yield return new ValidationResult("Las fechas requerida y de envío no pueden ser anteriores a la orden.");
    }
}
