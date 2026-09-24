using System.ComponentModel.DataAnnotations;

namespace Finanzauto.Application.Catalog;

public sealed class GenerateProductsRequest : IValidatableObject
{
    [Range(1, 100_000)]
    public int Count { get; set; } = 100_000;

    [Required, MinLength(1), MaxLength(100)]
    public int[] CategoryIds { get; set; } = [];

    [Range(1, int.MaxValue)]
    public int SupplierId { get; set; }

    [Required, StringLength(100)]
    public string NamePrefix { get; set; } = "Producto";

    [Range(typeof(decimal), "0", "1000000000")]
    public decimal MinPrice { get; set; } = 1;

    [Range(typeof(decimal), "0", "1000000000")]
    public decimal MaxPrice { get; set; } = 10000;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (CategoryIds is not null && (CategoryIds.Any(id => id <= 0) || CategoryIds.Distinct().Count() != CategoryIds.Length))
        {
            yield return new ValidationResult("CategoryIds debe contener identificadores positivos sin duplicados.", [nameof(CategoryIds)]);
        }

        if (MinPrice > MaxPrice || decimal.Round(MinPrice, 2) != MinPrice || decimal.Round(MaxPrice, 2) != MaxPrice)
        {
            yield return new ValidationResult("Los precios deben estar ordenados y tener máximo dos decimales.", [nameof(MinPrice), nameof(MaxPrice)]);
        }
    }
}
