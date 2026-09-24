using Finanzauto.Domain.Entities;

namespace Finanzauto.Application.Catalog;

public sealed class RandomProductGenerator : IRandomProductGenerator
{
    public IEnumerable<Product> Generate(GenerateProductsRequest request, Guid generationId)
    {
        // Trabajar en centavos limita los precios generados a dos decimales.
        var minimumPriceInCents = (long)(request.MinPrice * 100);
        var maximumPriceInCents = (long)(request.MaxPrice * 100);
        for (var index = 0; index < request.Count; index++)
        {
            // El resto de la división recorre las categorías y vuelve al inicio.
            var categoryIndex = index % request.CategoryIds.Length;

            // yield return entrega una fila por iteración; no construye una lista completa.
            yield return new Product
            {
                ProductName = $"{request.NamePrefix.Trim()}-{generationId:N}-{index + 1}",
                CategoryId = request.CategoryIds[categoryIndex],
                SupplierId = request.SupplierId,
                QuantityPerUnit = "1 unidad",
                // NextInt64 excluye el extremo superior; +1 permite incluir el precio máximo.
                UnitPrice = Random.Shared.NextInt64(minimumPriceInCents, maximumPriceInCents + 1) / 100m,
                UnitsInStock = Random.Shared.Next(0, 1001),
                UnitsOnOrder = Random.Shared.Next(0, 101),
                ReorderLevel = Random.Shared.Next(0, 51),
                Discontinued = false
            };
        }
    }
}
