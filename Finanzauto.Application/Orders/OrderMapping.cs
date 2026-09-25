using Finanzauto.Domain.Entities;

namespace Finanzauto.Application.Orders;

public static class OrderMapping
{
    public static OrderResponse ToResponse(this Order entity)
    {
        var details = entity.OrderDetails.Where(x => x.Active).OrderBy(x => x.OrderDetailId)
            .Select(x => new OrderDetailResponse
            {
                OrderDetailId = x.OrderDetailId,
                ProductId = x.ProductId,
                UnitPrice = x.UnitPrice,
                Quantity = x.Quantity,
                Discount = x.Discount,
                Total = decimal.Round(x.Quantity * x.UnitPrice * (1 - x.Discount), 2, MidpointRounding.AwayFromZero),
                CreationDate = x.CreationDate,
                UpdatedDate = x.UpdatedDate
            }).ToArray();

        return new OrderResponse
        {
            Id = entity.OrderId,
            CustomerId = entity.CustomerId,
            EmployeeId = entity.EmployeeId,
            ShipVia = entity.ShipVia,
            OrderDate = entity.OrderDate,
            RequiredDate = entity.RequiredDate,
            ShippedDate = entity.ShippedDate,
            Freight = entity.Freight,
            ShipName = entity.ShipName,
            ShipAddress = entity.ShipAddress,
            ShipCity = entity.ShipCity,
            ShipRegion = entity.ShipRegion,
            ShipPostalCode = entity.ShipPostalCode,
            ShipCountry = entity.ShipCountry,
            CreationDate = entity.CreationDate,
            UpdatedDate = entity.UpdatedDate,
            Details = details,
            Total = details.Sum(x => x.Total) + entity.Freight
        };
    }

    public static void Apply(Order entity, OrderRequest request)
    {
        entity.CustomerId = request.CustomerId;
        entity.ShipVia = request.ShipVia;
        entity.OrderDate = request.OrderDate;
        entity.RequiredDate = request.RequiredDate;
        entity.ShippedDate = request.ShippedDate;
        entity.Freight = request.Freight;
        entity.ShipName = request.ShipName?.Trim();
        entity.ShipAddress = request.ShipAddress?.Trim();
        entity.ShipCity = request.ShipCity?.Trim();
        entity.ShipRegion = request.ShipRegion?.Trim();
        entity.ShipPostalCode = request.ShipPostalCode?.Trim();
        entity.ShipCountry = request.ShipCountry?.Trim();
    }
}
