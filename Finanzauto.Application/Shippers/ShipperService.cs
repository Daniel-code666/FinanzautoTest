using Finanzauto.Application.Catalog;
using Finanzauto.Application.Common.Exceptions;
using Finanzauto.Domain.Entities;

namespace Finanzauto.Application.Shippers;

public sealed class ShipperService(IShipperStore store) : IShipperService
{
    public Task<CatalogPage<ShipperResponse>> ListAsync(CatalogQuery query, CancellationToken ct)
        => store.ListAsync(query, ct);

    private async Task<Shipper> ShipperAsync(int id, CancellationToken ct)
        => await store.FindAsync(id, ct) ?? throw new ApiException(404, "Transportadora no encontrada.");

    public async Task<ShipperResponse> GetAsync(int id, CancellationToken ct)
    {
        var entity = await ShipperAsync(id, ct);
        return entity.ToResponse();
    }

    public async Task<ShipperResponse> CreateAsync(ShipperRequest request, CancellationToken ct)
    {
        var entity = new Shipper();
        ShipperMapping.Apply(entity, request);
        await store.AddAsync(entity, ct);
        return entity.ToResponse();
    }

    public async Task<ShipperResponse> UpdateAsync(int id, ShipperRequest request, CancellationToken ct)
    {
        var entity = await ShipperAsync(id, ct);
        ShipperMapping.Apply(entity, request);
        await store.SaveAsync(ct);
        return entity.ToResponse();
    }

    public Task DeleteAsync(int id, CancellationToken ct)
        => store.DeleteAsync(id, ct);
}
