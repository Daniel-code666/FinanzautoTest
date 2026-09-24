using Finanzauto.Application.Common.Exceptions;
using Finanzauto.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Finanzauto.Infrastructure.Catalog;

internal static class CatalogReferenceGuard
{
    // Ejecutar dentro de una transacción. FOR SHARE impide desactivar/eliminar
    // las referencias mientras se crea o actualiza el producto.
    public static async Task LockActiveAsync(FinanzautoDbContext db, int[] categoryIds, int supplierId, CancellationToken ct)
    {
        await LockCategoriesAsync(db, categoryIds, ct);
        await LockSupplierAsync(db, supplierId, ct);
    }

    private static async Task LockCategoriesAsync(FinanzautoDbContext db, int[] categoryIds, CancellationToken ct)
    {
        // Un orden común reduce el riesgo de bloqueos cruzados entre cargas concurrentes.
        var distinctCategoryIds = categoryIds.Distinct().Order().ToArray();
        var activeCategoryIds = await db.Database.SqlQuery<int>($"""
            SELECT "CategoryId" AS "Value" FROM "Categories"
            WHERE "CategoryId" = ANY({distinctCategoryIds}) AND "Active"
            ORDER BY "CategoryId" FOR SHARE
            """).ToListAsync(ct);
        if (activeCategoryIds.Count != distinctCategoryIds.Length)
        {
            throw new ApiException(409, "Todas las categorías deben existir y estar activas.");
        }
    }

    private static async Task LockSupplierAsync(FinanzautoDbContext db, int supplierId, CancellationToken ct)
    {
        // Otra transacción puede leer estas filas, pero debe esperar para actualizarlas o borrarlas.
        var activeSupplierIds = await db.Database.SqlQuery<int>($"""
            SELECT "SupplierId" AS "Value" FROM "Suppliers"
            WHERE "SupplierId" = {supplierId} AND "Active" FOR SHARE
            """).ToListAsync(ct);
        if (activeSupplierIds.Count == 0)
        {
            throw new ApiException(409, "El proveedor debe existir y estar activo.");
        }
    }
}
