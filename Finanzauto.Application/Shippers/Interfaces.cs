using Finanzauto.Application.Catalog;
using Finanzauto.Domain.Entities;

namespace Finanzauto.Application.Shippers;

public interface IShipperStore
{
    Task<CatalogPage<ShipperResponse>> ListAsync(CatalogQuery query, CancellationToken ct);
    Task<Shipper?> FindAsync(int id, CancellationToken ct);
    Task AddAsync(Shipper entity, CancellationToken ct);
    Task SaveAsync(CancellationToken ct);
    Task DeleteAsync(int id, CancellationToken ct);
}

public interface IShipperService
{
    Task<CatalogPage<ShipperResponse>> ListAsync(CatalogQuery query, CancellationToken ct);
    Task<ShipperResponse> GetAsync(int id, CancellationToken ct);
    Task<ShipperResponse> CreateAsync(ShipperRequest request, CancellationToken ct);
    Task<ShipperResponse> UpdateAsync(int id, ShipperRequest request, CancellationToken ct);
    Task DeleteAsync(int id, CancellationToken ct);
}
