using System.ComponentModel.DataAnnotations;

namespace Finanzauto.Application.Catalog;

public class CatalogQuery
{
    [Range(1, 1_000_000)] public int Page { get; set; } = 1;
    [Range(1, 100)] public int PageSize { get; set; } = 20;
    [StringLength(100)] public string? Search { get; set; }
}

public enum ProductSort { Id, Name, Price }

public sealed class ProductQuery : CatalogQuery, IValidatableObject
{
    [Range(1, int.MaxValue)] public int? CategoryId { get; set; }
    [Range(1, int.MaxValue)] public int? SupplierId { get; set; }
    [Range(typeof(decimal), "0", "9999999999999999.99")] public decimal? MinPrice { get; set; }
    [Range(typeof(decimal), "0", "9999999999999999.99")] public decimal? MaxPrice { get; set; }
    public bool? InStock { get; set; }
    public bool? Discontinued { get; set; }
    [EnumDataType(typeof(ProductSort))] public ProductSort SortBy { get; set; } = ProductSort.Id;
    public bool Descending { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (MinPrice.HasValue && MaxPrice.HasValue && MinPrice > MaxPrice)
            yield return new("MinPrice no puede superar MaxPrice.", [nameof(MinPrice), nameof(MaxPrice)]);
    }
}

public sealed class ProductRequest : IValidatableObject
{
    [Required, StringLength(200)] public string ProductName { get; set; } = string.Empty;
    [Range(1, int.MaxValue)] public int CategoryId { get; set; }
    [Range(1, int.MaxValue)] public int SupplierId { get; set; }
    [StringLength(100)] public string? QuantityPerUnit { get; set; }
    [Range(typeof(decimal), "0", "9999999999999999.99")] public decimal UnitPrice { get; set; }
    [Range(0, int.MaxValue)] public int UnitsInStock { get; set; }
    [Range(0, int.MaxValue)] public int UnitsOnOrder { get; set; }
    [Range(0, int.MaxValue)] public int ReorderLevel { get; set; }
    public bool Discontinued { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (decimal.Round(UnitPrice, 2) != UnitPrice)
            yield return new("UnitPrice admite máximo dos decimales.", [nameof(UnitPrice)]);
    }
}

public sealed class CategoryRequest
{
    [Required, StringLength(100)] public string CategoryName { get; set; } = string.Empty;
    [StringLength(2000)] public string? Description { get; set; }
    [MaxLength(2 * 1024 * 1024)] public byte[]? Picture { get; set; }
}

public sealed class GenerateProductsRequest : IValidatableObject
{
    [Range(1, 100_000)] public int Count { get; set; } = 100_000;
    [Required, MinLength(1), MaxLength(100)] public int[] CategoryIds { get; set; } = [];
    [Range(1, int.MaxValue)] public int SupplierId { get; set; }
    [Required, StringLength(100)] public string NamePrefix { get; set; } = "Producto";
    [Range(typeof(decimal), "0", "1000000000")] public decimal MinPrice { get; set; } = 1;
    [Range(typeof(decimal), "0", "1000000000")] public decimal MaxPrice { get; set; } = 10000;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (CategoryIds is not null && (CategoryIds.Any(id => id <= 0) || CategoryIds.Distinct().Count() != CategoryIds.Length))
            yield return new("CategoryIds debe contener identificadores positivos sin duplicados.", [nameof(CategoryIds)]);
        if (MinPrice > MaxPrice || decimal.Round(MinPrice, 2) != MinPrice || decimal.Round(MaxPrice, 2) != MaxPrice)
            yield return new("Los precios deben estar ordenados y tener máximo dos decimales.", [nameof(MinPrice), nameof(MaxPrice)]);
    }
}

public sealed record CatalogPage<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize);
public sealed record CategoryResponse(int Id, string CategoryName, string? Description, bool HasPicture);
public sealed record CategoryDetailResponse(int Id, string CategoryName, string? Description,
    byte[]? Picture, string? PictureContentType);
public sealed record SupplierResponse(int Id, string CompanyName);
public sealed record ProductResponse(int Id, string ProductName, int CategoryId, string CategoryName,
    int SupplierId, string SupplierName, string? QuantityPerUnit, decimal UnitPrice, int UnitsInStock,
    int UnitsOnOrder, int ReorderLevel, bool Discontinued);
public sealed record ProductDetailResponse(ProductResponse Product, CategoryDetailResponse Category);
public sealed record GenerationResponse(Guid GenerationId, int CreatedCount, int[] CategoryIds, int SupplierId, long ElapsedMilliseconds);

