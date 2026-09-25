using Finanzauto.Application.Common;
using Finanzauto.Application.Catalog;
using Finanzauto.Domain.Entities;
using Finanzauto.Application.Common.Exceptions;
using Finanzauto.Application.Orders;
using Finanzauto.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Finanzauto.Infrastructure.Orders;

public sealed class OrderStore(FinanzautoDbContext db) : IOrderStore
{
    public async Task<CatalogPage<OrderResponse>> ListAsync(OrderQuery query, CancellationToken ct)
    {
        var rows = db.Orders.AsNoTracking();
        if (query.CustomerId.HasValue) rows = rows.Where(x => x.CustomerId == query.CustomerId);
        if (query.EmployeeId.HasValue) rows = rows.Where(x => x.EmployeeId == query.EmployeeId);
        if (query.ShipVia.HasValue) rows = rows.Where(x => x.ShipVia == query.ShipVia);
        if (query.FromDate.HasValue) rows = rows.Where(x => x.OrderDate >= query.FromDate);
        if (query.ToDate.HasValue) rows = rows.Where(x => x.OrderDate <= query.ToDate);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToUpperInvariant();
            rows = rows.Where(x => x.OrderId.ToString().Contains(term) ||
                (x.ShipName != null && x.ShipName.ToUpper().Contains(term)) ||
                (x.ShipCity != null && x.ShipCity.ToUpper().Contains(term)));
        }
        
        var total = await rows.CountAsync(ct);
        var offset = (query.Page - 1) * query.PageSize;
        var items = await rows.OrderBy(x => x.OrderId).Skip(offset).Take(query.PageSize).Include(x => x.OrderDetails.Where(d => d.Active)).AsSplitQuery().ToListAsync(ct);

        return new CatalogPage<OrderResponse>
        {
            Items = items.Select(x => x.ToResponse()).ToArray(),
            TotalCount = total,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }

    public Task<Order?> FindAsync(int id, CancellationToken ct)
        => db.Orders.Include(x => x.OrderDetails.Where(d => d.Active)).FirstOrDefaultAsync(x => x.OrderId == id, ct);

    public async Task ValidateReferencesAsync(int customerId, int? shipVia, int employeeId, CancellationToken ct)
    {
        if (!await db.Customers.AnyAsync(x => x.CustomerId == customerId, ct))
            throw new ApiException(409, "El cliente debe existir y estar activo.");
        if (shipVia.HasValue && !await db.Shippers.AnyAsync(x => x.ShipperId == shipVia, ct))
            throw new ApiException(409, "La transportadora debe existir y estar activa.");
        if (!await db.Employees.AnyAsync(x => x.EmployeeId == employeeId && x.Role.Active, ct))
            throw new ApiException(409, "El empleado y su rol deben estar activos.");
    }

    public async Task<IReadOnlyList<Product>> FindProductsAsync(int[] ids, CancellationToken ct)
        => await db.Products.AsNoTracking().Where(x => ids.Contains(x.ProductId) && x.Category.Active && x.Supplier.Active).ToListAsync(ct);

    public async Task AddAsync(Order order, CancellationToken ct)
    {
        db.Orders.Add(order);
        await SaveAsync(ct);
    }

    public async Task SaveAsync(CancellationToken ct)
    {
        // EF guarda cabecera y detalles en una sola transacción.
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new ApiException(409, "El producto ya tiene un detalle activo en esta orden.");
        }
    }

    public async Task DeactivateDetailAsync(int orderId, int detailId, CancellationToken ct)
    {
        var detail = await db.OrderDetails.FirstOrDefaultAsync(
            x => x.OrderDetailId == detailId && x.OrderId == orderId && x.Order.Active, ct)
            ?? throw new ApiException(404, "Detalle activo no encontrado en esta orden.");
        EntityStatus.SetActive(detail, false);
        await SaveAsync(ct);
    }

    public async Task DeactivateAsync(int id, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var order = await db.Orders.FirstOrDefaultAsync(x => x.OrderId == id, ct)
            ?? throw new ApiException(404, "Pedido no encontrado.");
        var details = await db.OrderDetails
            .Where(x => x.OrderId == id && x.Active).ToListAsync(ct);

        EntityStatus.SetActive(order, false);
        foreach (var detail in details)
        {
            EntityStatus.SetActive(detail, false);
        }

        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }
}
