using Finanzauto.Application.Common.Exceptions;
using Finanzauto.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Finanzauto.Infrastructure.Catalog;

internal static class CatalogReferenceGuard
{
    public static async Task ValidateAsync(FinanzautoDbContext db, int[] categoryIds, int supplierId, CancellationToken ct)
    {
        // Valida el estado actual sin bloquear cambios concurrentes.
        var distinctCategoryIds = categoryIds.Distinct().ToArray();
        var activeCategories = await db.Categories.CountAsync(category => distinctCategoryIds.Contains(category.CategoryId) && category.Active, ct);

        if (activeCategories != distinctCategoryIds.Length)
        {
            throw new ApiException(409, "Todas las categor\u00edas deben existir y estar activas.");
        }

        var supplierExists = await db.Suppliers.AnyAsync(supplier => supplier.SupplierId == supplierId && supplier.Active, ct);

        if (!supplierExists)
        {
            throw new ApiException(409, "El proveedor debe existir y estar activo.");
        }
    }
}
