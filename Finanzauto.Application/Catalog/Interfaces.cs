using Finanzauto.Domain.Entities;

namespace Finanzauto.Application.Catalog;

public interface ICatalogStore
{
    Task<CatalogPage<ProductResponse>> ListProductsAsync(ProductQuery query, CancellationToken ct);
    Task<ProductDetailResponse?> GetProductAsync(int id, CancellationToken ct);
    Task<int> SaveProductAsync(int? id, Product product, CancellationToken ct);
    Task DeactivateProductAsync(int id, CancellationToken ct);
    Task<CatalogPage<CategoryResponse>> ListCategoriesAsync(CatalogQuery query, CancellationToken ct);
    Task<CategoryDetailResponse?> GetCategoryAsync(int id, CancellationToken ct);
    Task<int> SaveCategoryAsync(int? id, Category category, CancellationToken ct);
    Task DeactivateCategoryAsync(int id, CancellationToken ct);
    Task<CatalogPage<SupplierResponse>> ListSuppliersAsync(CatalogQuery query, CancellationToken ct);
}

public interface IProductService
{
    Task<CatalogPage<ProductResponse>> ListAsync(ProductQuery query, CancellationToken ct);
    Task<ProductDetailResponse> GetAsync(int id, CancellationToken ct);
    Task<ProductDetailResponse> CreateAsync(ProductRequest request, CancellationToken ct);
    Task<ProductDetailResponse> UpdateAsync(int id, ProductRequest request, CancellationToken ct);
    Task DeleteAsync(int id, CancellationToken ct);
}

public interface ICategoryService
{
    Task<CatalogPage<CategoryResponse>> ListAsync(CatalogQuery query, CancellationToken ct);
    Task<CategoryDetailResponse> GetAsync(int id, CancellationToken ct);
    Task<CategoryDetailResponse> CreateAsync(CategoryRequest request, CancellationToken ct);
    Task<CategoryDetailResponse> UpdateAsync(int id, CategoryRequest request, CancellationToken ct);
    Task DeleteAsync(int id, CancellationToken ct);
}

public interface IRandomProductGenerator
{
    IEnumerable<Product> Generate(GenerateProductsRequest request, Guid generationId);
}

public interface IBulkProductWriter
{
    Task<int> WriteAsync(IEnumerable<Product> products, int[] categoryIds, int supplierId, CancellationToken ct);
}

public interface IProductGenerationService
{
    Task<GenerationResponse> GenerateAsync(GenerateProductsRequest request, CancellationToken ct);
}

