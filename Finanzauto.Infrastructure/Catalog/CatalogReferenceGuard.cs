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
        var ids = categoryIds.Distinct().Order().ToArray();
        var found = await db.Database.SqlQuery<int>($"""
            SELECT "CategoryId" AS "Value" FROM "Categories"
            WHERE "CategoryId" = ANY({ids}) AND "Active"
            ORDER BY "CategoryId" FOR SHARE
            """).ToListAsync(ct);
        if (found.Count != ids.Length)
            throw new ApiException(409, "Todas las categorías deben existir y estar activas.");

        var suppliers = await db.Database.SqlQuery<int>($"""
            SELECT "SupplierId" AS "Value" FROM "Suppliers"
            WHERE "SupplierId" = {supplierId} AND "Active" FOR SHARE
            """).ToListAsync(ct);
        if (suppliers.Count == 0)
            throw new ApiException(409, "El proveedor debe existir y estar activo.");
    }
}

