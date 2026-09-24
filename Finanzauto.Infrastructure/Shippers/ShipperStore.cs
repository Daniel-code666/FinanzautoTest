using Finanzauto.Application.Catalog;
using Finanzauto.Application.Common;
using Finanzauto.Application.Common.Exceptions;
using Finanzauto.Application.Shippers;
using Finanzauto.Domain.Entities;
using Finanzauto.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Finanzauto.Infrastructure.Shippers;

public sealed class ShipperStore(FinanzautoDbContext db) : IShipperStore
{
    public async Task<CatalogPage<ShipperResponse>> ListAsync(CatalogQuery query, CancellationToken ct)
    {
        var rows = db.Shippers.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToUpperInvariant();
            rows = rows.Where(x => x.CompanyName.ToUpper().Contains(term) || (x.Phone != null && x.Phone.Contains(term)));
        }

        var total = await rows.CountAsync(ct);
        var offset = (query.Page - 1) * query.PageSize;
        var items = await rows.OrderBy(x => x.ShipperId).Skip(offset).Take(query.PageSize)
            .Select(x => new ShipperResponse
            {
                Id = x.ShipperId,
                CompanyName = x.CompanyName,
                Phone = x.Phone,
                CreationDate = x.CreationDate,
                UpdatedDate = x.UpdatedDate
            }).ToListAsync(ct);

        return new CatalogPage<ShipperResponse>
        {
            Items = items,
            TotalCount = total,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }

    public Task<Shipper?> FindAsync(int id, CancellationToken ct)
        => db.Shippers.FirstOrDefaultAsync(x => x.ShipperId == id, ct);

    public async Task AddAsync(Shipper entity, CancellationToken ct)
    {
        db.Shippers.Add(entity);
        await SaveAsync(ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct)
    {
        var entity = await FindAsync(id, ct) ?? throw new ApiException(404, "Transportadora no encontrada.");
        if (await db.Orders.AnyAsync(x => x.ShipVia == id && x.Active, ct))
            throw new ApiException(409, "La transportadora tiene pedidos activos.");

        EntityStatus.SetActive(entity, false);
        await SaveAsync(ct);
    }

    public async Task SaveAsync(CancellationToken ct)
    {
        await db.SaveChangesAsync(ct);
    }
}
