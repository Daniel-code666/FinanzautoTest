using System.ComponentModel.DataAnnotations;

namespace Finanzauto.Application.Catalog;

public sealed class ProductRequest : IValidatableObject
{
    [Required, StringLength(200)]
    public string ProductName { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int CategoryId { get; set; }

    [Range(1, int.MaxValue)]
    public int SupplierId { get; set; }

    [StringLength(100)]
    public string? QuantityPerUnit { get; set; }

    [Range(typeof(decimal), "0", "9999999999999999.99", ParseLimitsInInvariantCulture = true)]
    public decimal UnitPrice { get; set; }

    [Range(0, int.MaxValue)]
    public int UnitsInStock { get; set; }

    [Range(0, int.MaxValue)]
    public int UnitsOnOrder { get; set; }

    [Range(0, int.MaxValue)]
    public int ReorderLevel { get; set; }
    public bool Discontinued { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (decimal.Round(UnitPrice, 2) != UnitPrice)
        {
            yield return new ValidationResult("UnitPrice admite máximo dos decimales.", [nameof(UnitPrice)]);
        }
    }
}
