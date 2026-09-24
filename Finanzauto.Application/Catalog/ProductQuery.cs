using System.ComponentModel.DataAnnotations;

namespace Finanzauto.Application.Catalog;

public sealed class ProductQuery : CatalogQuery, IValidatableObject
{
    [Range(1, int.MaxValue)]
    public int? CategoryId { get; set; }

    [Range(1, int.MaxValue)]
    public int? SupplierId { get; set; }

    [Range(typeof(decimal), "0", "9999999999999999.99", ParseLimitsInInvariantCulture = true)]
    public decimal? MinPrice { get; set; }

    [Range(typeof(decimal), "0", "9999999999999999.99", ParseLimitsInInvariantCulture = true)]
    public decimal? MaxPrice { get; set; }
    public bool? InStock { get; set; }
    public bool? Discontinued { get; set; }

    [EnumDataType(typeof(ProductSort))]
    public ProductSort SortBy { get; set; } = ProductSort.Id;
    public bool Descending { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (MinPrice.HasValue && MaxPrice.HasValue && MinPrice > MaxPrice)
        {
            yield return new ValidationResult("MinPrice no puede superar MaxPrice.", [nameof(MinPrice), nameof(MaxPrice)]);
        }
    }
}
