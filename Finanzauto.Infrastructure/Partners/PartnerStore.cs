using Finanzauto.Application.Catalog;
using Finanzauto.Application.Common.Exceptions;
using Finanzauto.Application.Partners;
using Finanzauto.Domain.Entities;
using Finanzauto.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Finanzauto.Infrastructure.Partners;

public sealed class PartnerStore(FinanzautoDbContext db) : IPartnerStore
{

    public async Task<CatalogPage<SupplierDetailResponse>> ListSuppliersAsync(PartnerQuery query, CancellationToken ct)
    {
        var rows = db.Suppliers.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToUpperInvariant();
            rows = rows.Where(x => x.CompanyName.ToUpper().Contains(term) ||
                (x.ContactName != null && x.ContactName.ToUpper().Contains(term)));
        }
        if (!string.IsNullOrWhiteSpace(query.Country))
        {
            var country = query.Country.Trim().ToUpperInvariant();
            rows = rows.Where(x => x.Country != null && x.Country.ToUpper() == country);
        }
        if (!string.IsNullOrWhiteSpace(query.City))
        {
            var city = query.City.Trim().ToUpperInvariant();
            rows = rows.Where(x => x.City != null && x.City.ToUpper() == city);
        }
        var total = await rows.CountAsync(ct);
        var offset = (query.Page - 1) * query.PageSize;
        var items = await rows
            .OrderBy(x => x.SupplierId)
            .Skip(offset)
            .Take(query.PageSize)
            .Select(x => new SupplierDetailResponse
            {
                Id = x.SupplierId,
                CompanyName = x.CompanyName,
                ContactName = x.ContactName,
                ContactTitle = x.ContactTitle,
                Address = x.Address,
                City = x.City,
                Region = x.Region,
                PostalCode = x.PostalCode,
                Country = x.Country,
                Phone = x.Phone,
                Fax = x.Fax,
                HomePage = x.HomePage,
                CreationDate = x.CreationDate,
                UpdatedDate = x.UpdatedDate
            })
            .ToListAsync(ct);
        return new CatalogPage<SupplierDetailResponse>
        {
            Items = items,
            TotalCount = total,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }

    public async Task<CatalogPage<CustomerResponse>> ListCustomersAsync(PartnerQuery query, CancellationToken ct)
    {
        var rows = db.Customers.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToUpperInvariant();
            rows = rows.Where(x => x.CompanyName.ToUpper().Contains(term) ||
                (x.ContactName != null && x.ContactName.ToUpper().Contains(term)) || x.CustomerId.ToUpper().Contains(term));
        }
        if (!string.IsNullOrWhiteSpace(query.Country))
        {
            var country = query.Country.Trim().ToUpperInvariant();
            rows = rows.Where(x => x.Country != null && x.Country.ToUpper() == country);
        }
        if (!string.IsNullOrWhiteSpace(query.City))
        {
            var city = query.City.Trim().ToUpperInvariant();
            rows = rows.Where(x => x.City != null && x.City.ToUpper() == city);
        }
        var total = await rows.CountAsync(ct);
        var offset = (query.Page - 1) * query.PageSize;
        var items = await rows
            .OrderBy(x => x.CustomerId)
            .Skip(offset)
            .Take(query.PageSize)
            .Select(x => new CustomerResponse
            {
                Id = x.CustomerId,
                CompanyName = x.CompanyName,
                ContactName = x.ContactName,
                ContactTitle = x.ContactTitle,
                Address = x.Address,
                City = x.City,
                Region = x.Region,
                PostalCode = x.PostalCode,
                Country = x.Country,
                Phone = x.Phone,
                Fax = x.Fax,
                CreationDate = x.CreationDate,
                UpdatedDate = x.UpdatedDate
            })
            .ToListAsync(ct);
        return new CatalogPage<CustomerResponse>
        {
            Items = items,
            TotalCount = total,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }
    public Task<Supplier?> FindSupplierAsync(int id, CancellationToken ct) =>
        db.Suppliers.SingleOrDefaultAsync(x => x.SupplierId == id, ct);
    public Task<Customer?> FindCustomerAsync(string id, CancellationToken ct) =>
        db.Customers.SingleOrDefaultAsync(x => x.CustomerId == id, ct);

    public async Task AddSuppliersAsync(IReadOnlyList<Supplier> items, CancellationToken ct)
    {
        // Un único SaveChanges usa una transacción para todos los comandos del lote.
        db.Suppliers.AddRange(items);
        await SaveAsync(ct);
    }
    public async Task AddCustomersAsync(IReadOnlyList<Customer> items, CancellationToken ct)
    {
        var ids = items.Select(x => x.CustomerId).ToArray();
        if (await db.Customers.IgnoreQueryFilters().AnyAsync(x => ids.Contains(x.CustomerId), ct))
            throw new ApiException(409, "Un código de cliente ya existe, incluso si está inactivo.");
        db.Customers.AddRange(items);
        await SaveAsync(ct);
    }
    public async Task DeleteSupplierAsync(int id, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var suppliers = await db.Suppliers.FromSql($"""
            SELECT * FROM "Suppliers" WHERE "SupplierId" = {id} AND "Active" FOR UPDATE
            """).ToListAsync(ct);
        var entity = suppliers.SingleOrDefault() ?? throw new ApiException(404, "Proveedor no encontrado.");
        if (await db.Products.AnyAsync(x => x.SupplierId == id, ct))
            throw new ApiException(409, "Reasigna o desactiva los productos activos antes de eliminar el proveedor.");
        db.Suppliers.Remove(entity);
        await SaveAsync(ct);
        await transaction.CommitAsync(ct);
    }
    public async Task DeleteCustomerAsync(string id, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var customers = await db.Customers.FromSql($"""
            SELECT * FROM "Customers" WHERE "CustomerId" = {id} AND "Active" FOR UPDATE
            """).ToListAsync(ct);
        var entity = customers.SingleOrDefault() ?? throw new ApiException(404, "Cliente no encontrado.");
        if (await db.Orders.AnyAsync(x => x.CustomerId == id, ct))
            throw new ApiException(409, "El cliente tiene pedidos activos.");
        db.Customers.Remove(entity);
        await SaveAsync(ct);
        await transaction.CommitAsync(ct);
    }
    public async Task SaveAsync(CancellationToken ct)
    {
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new ApiException(409, "Ya existe un registro con ese identificador. No se guardó el lote.");
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ApiException(409, "El registro cambió o fue eliminado. Actualiza los datos e intenta nuevamente.");
        }
    }
}

