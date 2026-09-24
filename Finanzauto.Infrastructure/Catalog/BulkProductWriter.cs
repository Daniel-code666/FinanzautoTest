using Finanzauto.Application.Catalog;
using Finanzauto.Domain.Entities;
using Finanzauto.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Finanzauto.Infrastructure.Catalog;

public sealed class BulkProductWriter(FinanzautoDbContext db) : IBulkProductWriter
{
    public async Task<int> WriteAsync(IEnumerable<Product> products, int[] categoryIds, int supplierId, CancellationToken ct)
    {
        // Si falla un lote, la misma transaccion revierte todos los anteriores.
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await CatalogReferenceGuard.ValidateAsync(db, categoryIds, supplierId, ct);
        var insertedCount = 0;
        foreach (var batch in products.Chunk(1000))
        {
            ct.ThrowIfCancellationRequested();
            db.Products.AddRange(batch);
            await db.SaveChangesAsync(ct);
            insertedCount += batch.Length;

            // Liberar solo los productos guardados evita acumular toda la carga en EF.
            foreach (var product in batch)
            {
                db.Entry(product).State = EntityState.Detached;
            }
        }
        await transaction.CommitAsync(ct);
        return insertedCount;
    }
}
