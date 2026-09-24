using Finanzauto.Application.Common;
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
            rows = rows.Where(x => x.CompanyName.ToUpper().Contains(term) || (x.ContactName != null && x.ContactName.ToUpper().Contains(term))
                || x.CustomerId.ToString().Contains(term));
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
        var items = await rows.OrderBy(x => x.CustomerId).Skip(offset).Take(query.PageSize)
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

    public Task<Supplier?> FindSupplierAsync(int id, CancellationToken ct)
        => db.Suppliers.FirstOrDefaultAsync(x => x.SupplierId == id, ct);

    public Task<Customer?> FindCustomerAsync(int id, CancellationToken ct)
        => db.Customers.FirstOrDefaultAsync(x => x.CustomerId == id, ct);

    public async Task AddSuppliersAsync(IReadOnlyList<Supplier> items, CancellationToken ct)
    {
        // Un único SaveChanges usa una transacción para todos los comandos del lote.
        db.Suppliers.AddRange(items);
        await SaveAsync(ct);
    }

    public async Task AddCustomersAsync(IReadOnlyList<Customer> items, CancellationToken ct)
    {
        db.Customers.AddRange(items);

        await SaveAsync(ct);
    }

    public async Task DeleteSupplierAsync(int id, CancellationToken ct)
    {
        var entity = db.Suppliers.FirstOrDefault(x => x.SupplierId == id) ?? throw new ApiException(404, "Proveedor no encontrado.");

        if (await db.Products.AnyAsync(x => x.SupplierId == id, ct))
            throw new ApiException(409, "Reasigna o desactiva los productos activos antes de eliminar el proveedor.");

        EntityStatus.SetActive(entity, false);

        await SaveAsync(ct);
    }

    public async Task DeleteCustomerAsync(int id, CancellationToken ct)
    {
        var entity = await db.Customers.Where(x => x.CustomerId == id).FirstOrDefaultAsync(ct) ?? throw new ApiException(404, "Cliente no encontrado.");

        if (await db.Orders.AnyAsync(x => x.CustomerId == id, ct))
            throw new ApiException(409, "El cliente tiene pedidos activos.");

        EntityStatus.SetActive(entity, false);

        await SaveAsync(ct);
    }

    public async Task SaveAsync(CancellationToken ct)
    {
        await db.SaveChangesAsync(ct);
    }
}

