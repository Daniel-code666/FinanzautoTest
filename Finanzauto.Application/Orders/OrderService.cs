using System.ComponentModel.DataAnnotations;
using Finanzauto.Application.Catalog;
using Finanzauto.Application.Common.Exceptions;
using Finanzauto.Domain.Entities;

namespace Finanzauto.Application.Orders;

public sealed class OrderService(IOrderStore store) : IOrderService
{
    public Task<CatalogPage<OrderResponse>> ListAsync(OrderQuery query, CancellationToken ct)
        => store.ListAsync(query, ct);

    private async Task<Order> OrderAsync(int id, CancellationToken ct)
        => await store.FindAsync(id, ct) ?? throw new ApiException(404, "Pedido no encontrado.");

    public async Task<OrderResponse> GetAsync(int id, CancellationToken ct)
    {
        var entity = await OrderAsync(id, ct);
        return entity.ToResponse();
    }

    public async Task<OrderResponse> CreateAsync(OrderRequest request, int employeeId, CancellationToken ct)
    {
        var products = await ValidateCreateAsync(request, employeeId, ct);
        var entity = new Order
        {
            EmployeeId = employeeId,
            OrderDetails = [.. request.Details.Select(detail => new OrderDetail
            {
                ProductId = detail.ProductId,
                Quantity = detail.Quantity,
                Discount = detail.Discount,
                UnitPrice = products.Single(product => product.ProductId == detail.ProductId).UnitPrice
            })]
        };

        OrderMapping.Apply(entity, request);

        await store.AddAsync(entity, ct);

        return entity.ToResponse();
    }

    public async Task<OrderResponse> UpdateAsync(int id, OrderRequest request, CancellationToken ct)
    {
        Validate(request);
        var entity = await OrderAsync(id, ct);
        var products = await ValidateUpdateAsync(entity, request, ct);

        // Toda la validación termina antes de modificar las entidades rastreadas por EF.
        // Los detalles omitidos permanecen intactos.
        foreach (var item in request.Details)
        {
            OrderDetail detail;
            if (item.OrderDetailId.HasValue)
            {
                detail = entity.OrderDetails.Single(x => x.Active && x.OrderDetailId == item.OrderDetailId);
            }
            else
            {
                var product = products.Single(x => x.ProductId == item.ProductId);
                detail = new OrderDetail { ProductId = product.ProductId, UnitPrice = product.UnitPrice };
                entity.OrderDetails.Add(detail);
            }
            detail.Quantity = item.Quantity;
            detail.Discount = item.Discount;
        }
        OrderMapping.Apply(entity, request);
        await store.SaveAsync(ct);
        return entity.ToResponse();
    }

    public Task DeleteAsync(int id, CancellationToken ct)
        => store.DeactivateAsync(id, ct);

    public Task DeleteDetailAsync(int orderId, int detailId, CancellationToken ct)
        => store.DeactivateDetailAsync(orderId, detailId, ct);

    private async Task<IReadOnlyList<Product>> ValidateCreateAsync(OrderRequest request, int employeeId, CancellationToken ct)
    {
        Validate(request);
        if (request.Details.Any(x => x.OrderDetailId.HasValue))
            throw new ApiException(400, "Una orden nueva no admite IDs de detalle.");

        await store.ValidateReferencesAsync(request.CustomerId, request.ShipVia, employeeId, ct);
        var productIds = request.Details.Select(x => x.ProductId).ToArray();
        var products = await store.FindProductsAsync(productIds, ct);
        if (products.Count != productIds.Length)
            throw new ApiException(409, "Los productos nuevos y sus categorías y proveedores deben estar activos.");
        return products;
    }

    private async Task<IReadOnlyList<Product>> ValidateUpdateAsync(Order entity, OrderRequest orderRequest, CancellationToken ct)
    {
        await store.ValidateReferencesAsync(orderRequest.CustomerId, orderRequest.ShipVia, entity.EmployeeId, ct);
        var requests = orderRequest.Details;
        var activeDetails = entity.OrderDetails.Where(x => x.Active).ToList();
        foreach (var request in requests.Where(x => x.OrderDetailId.HasValue))
        {
            var detail = activeDetails.FirstOrDefault(x => x.OrderDetailId == request.OrderDetailId)
                ?? throw new ApiException(404, "El detalle no existe o no pertenece a la orden activa.");
            if (detail.ProductId != request.ProductId)
                throw new ApiException(409, "No se puede cambiar el producto de un detalle; inactívalo y agrega uno nuevo.");
        }

        var newRequests = requests.Where(x => !x.OrderDetailId.HasValue).ToArray();
        if (activeDetails.Count + newRequests.Length > 1000)
            throw new ApiException(400, "Una orden admite hasta 1000 detalles activos.");
        if (newRequests.Any(x => activeDetails.Any(d => d.ProductId == x.ProductId)))
            throw new ApiException(409, "El producto ya tiene un detalle activo en esta orden.");

        var products = await store.FindProductsAsync(newRequests.Select(x => x.ProductId).ToArray(), ct);
        if (products.Count != newRequests.Length)
            throw new ApiException(409, "Los productos nuevos y sus categorías y proveedores deben estar activos.");

        return products;
    }

    private static void Validate(OrderRequest request)
    {
        var errors = new List<ValidationResult>();
        if (!Validator.TryValidateObject(request, new ValidationContext(request), errors, true))
            throw new ApiException(400, string.Join(" ", errors.Select(x => x.ErrorMessage)));
        foreach (var detail in request.Details)
        {
            if (detail == null)
                throw new ApiException(400, "Los detalles no pueden ser nulos.");
            if (!Validator.TryValidateObject(detail, new ValidationContext(detail), errors, true))
                throw new ApiException(400, string.Join(" ", errors.Select(x => x.ErrorMessage)));
            if (decimal.Round(detail.Discount, 4) != detail.Discount)
                throw new ApiException(400, "Discount admite hasta cuatro decimales.");
        }
        if (request.Details.Select(x => x.ProductId).Distinct().Count() != request.Details.Length)
            throw new ApiException(409, "El arreglo contiene productos duplicados.");
        var ids = request.Details.Where(x => x.OrderDetailId.HasValue).Select(x => x.OrderDetailId).ToArray();
        if (ids.Distinct().Count() != ids.Length)
            throw new ApiException(409, "El arreglo contiene IDs de detalle duplicados.");
    }
}
