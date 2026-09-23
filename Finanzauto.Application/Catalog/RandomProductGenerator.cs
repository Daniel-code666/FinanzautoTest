using Finanzauto.Domain.Entities;

namespace Finanzauto.Application.Catalog;

public sealed class RandomProductGenerator : IRandomProductGenerator
{
    public IEnumerable<Product> Generate(GenerateProductsRequest request, Guid generationId)
    {
        var minCents = (long)(request.MinPrice * 100);
        var maxCents = (long)(request.MaxPrice * 100);
        for (var index = 0; index < request.Count; index++)
        {
            yield return new Product
            {
                ProductName = $"{request.NamePrefix.Trim()}-{generationId:N}-{index + 1}",
                CategoryId = request.CategoryIds[index % request.CategoryIds.Length],
                SupplierId = request.SupplierId,
                QuantityPerUnit = "1 unidad",
                UnitPrice = Random.Shared.NextInt64(minCents, maxCents + 1) / 100m,
                UnitsInStock = Random.Shared.Next(0, 1001),
                UnitsOnOrder = Random.Shared.Next(0, 101),
                ReorderLevel = Random.Shared.Next(0, 51),
                Discontinued = false
            };
        }
    }
}

