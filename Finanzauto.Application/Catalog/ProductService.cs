using Finanzauto.Application.Common.Exceptions;
using Finanzauto.Domain.Entities;

namespace Finanzauto.Application.Catalog;

public sealed class ProductService(ICatalogStore store) : IProductService
{
    public Task<CatalogPage<ProductResponse>> ListAsync(ProductQuery query, CancellationToken ct)
    {
        return store.ListProductsAsync(query, ct);
    }

    public async Task<ProductDetailResponse> GetAsync(int id, CancellationToken ct)
    {
        var product = await store.GetProductAsync(id, ct);
        if (product == null)
        {
            throw new ApiException(404, "Producto no encontrado.");
        }
        return product;
    }

    public async Task<ProductDetailResponse> CreateAsync(ProductRequest request, CancellationToken ct)
    {
        var id = await store.SaveProductAsync(null, Map(request), ct);
        return await GetAsync(id, ct);
    }

    public async Task<ProductDetailResponse> UpdateAsync(int id, ProductRequest request, CancellationToken ct)
    {
        await store.SaveProductAsync(id, Map(request), ct);
        return await GetAsync(id, ct);
    }

    public Task DeleteAsync(int id, CancellationToken ct)
    {
        return store.DeactivateProductAsync(id, ct);
    }

    private static Product Map(ProductRequest request)
    {
        return new Product()
        {
            ProductName = request.ProductName.Trim(),
            CategoryId = request.CategoryId,
            SupplierId = request.SupplierId,
            QuantityPerUnit = request.QuantityPerUnit?.Trim(),
            UnitPrice = request.UnitPrice,
            UnitsInStock = request.UnitsInStock,
            UnitsOnOrder = request.UnitsOnOrder,
            ReorderLevel = request.ReorderLevel,
            Discontinued = request.Discontinued
        };
    }
}
