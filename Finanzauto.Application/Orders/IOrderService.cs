using Finanzauto.Application.Catalog;

namespace Finanzauto.Application.Orders;

public interface IOrderService
{
    Task<CatalogPage<OrderResponse>> ListAsync(OrderQuery query, CancellationToken ct);
    Task<OrderResponse> GetAsync(int id, CancellationToken ct);
    Task<OrderResponse> CreateAsync(OrderRequest request, int employeeId, CancellationToken ct);
    Task<OrderResponse> UpdateAsync(int id, OrderRequest request, CancellationToken ct);
    Task DeleteAsync(int id, CancellationToken ct);
    Task DeleteDetailAsync(int orderId, int detailId, CancellationToken ct);
}
