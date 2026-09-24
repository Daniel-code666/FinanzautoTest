using Finanzauto.Domain.Entities;

namespace Finanzauto.Application.Shippers;

public static class ShipperMapping
{
    public static ShipperResponse ToResponse(this Shipper entity)
    {
        return new ShipperResponse
        {
            Id = entity.ShipperId,
            CompanyName = entity.CompanyName,
            Phone = entity.Phone,
            CreationDate = entity.CreationDate,
            UpdatedDate = entity.UpdatedDate
        };
    }

    public static void Apply(Shipper entity, ShipperRequest request)
    {
        entity.CompanyName = request.CompanyName.Trim();
        entity.Phone = request.Phone?.Trim();
    }
}
