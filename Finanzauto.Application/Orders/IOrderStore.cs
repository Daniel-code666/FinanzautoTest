using Finanzauto.Application.Catalog;
using Finanzauto.Domain.Entities;

namespace Finanzauto.Application.Orders;

public interface IOrderStore
{
    Task<CatalogPage<OrderResponse>> ListAsync(OrderQuery query, CancellationToken ct);
    Task<Order?> FindAsync(int id, CancellationToken ct);
    Task ValidateReferencesAsync(int customerId, int? shipVia, int employeeId, CancellationToken ct);
    Task<IReadOnlyList<Product>> FindProductsAsync(int[] ids, CancellationToken ct);
    Task AddAsync(Order order, CancellationToken ct);
    Task SaveAsync(CancellationToken ct);
    Task DeactivateAsync(int id, CancellationToken ct);
    Task DeactivateDetailAsync(int orderId, int detailId, CancellationToken ct);
}
