using Finanzauto.Application.Catalog;
using Finanzauto.Domain.Entities;
using Finanzauto.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;

namespace Finanzauto.Infrastructure.Catalog;

public sealed class BulkProductWriter(FinanzautoDbContext db) : IBulkProductWriter
{
    public async Task<int> WriteAsync(IEnumerable<Product> products, int[] categoryIds, int supplierId, CancellationToken ct)
    {
        // La validación, los bloqueos y la carga pertenecen a la misma transacción.
        // Si una fila falla, salir sin Commit revierte todo el lote.
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await CatalogReferenceGuard.LockActiveAsync(db, categoryIds, supplierId, ct);

        var connection = (NpgsqlConnection)db.Database.GetDbConnection();
        var insertedCount = await CopyProductsAsync(connection, products, ct);

        await transaction.CommitAsync(ct);
        return insertedCount;
    }

    private static async Task<int> CopyProductsAsync(
        NpgsqlConnection connection, IEnumerable<Product> products, CancellationToken ct)
    {
        // COPY envía filas directamente a PostgreSQL sin registrar 100.000 entidades en EF.
        // El orden y los tipos de WriteAsync deben coincidir con las columnas del COPY.
        int count;
        await using (var writer = await connection.BeginBinaryImportAsync("""
            COPY "Products" ("ProductName", "SupplierId", "CategoryId", "QuantityPerUnit",
                "UnitPrice", "UnitsInStock", "UnitsOnOrder", "ReorderLevel", "Discontinued", "Active")
            FROM STDIN (FORMAT BINARY)
            """, ct))
        {
            foreach (var product in products)
            {
                // El generador entrega el siguiente producto al avanzar este foreach.
                ct.ThrowIfCancellationRequested();
                await writer.StartRowAsync(ct);
                await writer.WriteAsync(product.ProductName, NpgsqlDbType.Varchar, ct);
                await writer.WriteAsync(product.SupplierId, NpgsqlDbType.Integer, ct);
                await writer.WriteAsync(product.CategoryId, NpgsqlDbType.Integer, ct);
                await writer.WriteAsync(product.QuantityPerUnit, NpgsqlDbType.Varchar, ct);
                await writer.WriteAsync(product.UnitPrice, NpgsqlDbType.Numeric, ct);
                await writer.WriteAsync(product.UnitsInStock, NpgsqlDbType.Integer, ct);
                await writer.WriteAsync(product.UnitsOnOrder, NpgsqlDbType.Integer, ct);
                await writer.WriteAsync(product.ReorderLevel, NpgsqlDbType.Integer, ct);
                await writer.WriteAsync(product.Discontinued, NpgsqlDbType.Boolean, ct);
                await writer.WriteAsync(true, NpgsqlDbType.Boolean, ct);
            }
            // Complete termina COPY; todavía falta confirmar la transacción exterior.
            count = checked((int)await writer.CompleteAsync(ct));
        }
        return count;
    }
}
