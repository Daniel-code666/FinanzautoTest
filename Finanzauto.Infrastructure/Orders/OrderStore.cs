using Finanzauto.Application.Common;
using Finanzauto.Application.Common.Exceptions;
using Finanzauto.Application.Orders;
using Finanzauto.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Finanzauto.Infrastructure.Orders;

public sealed class OrderStore(FinanzautoDbContext db) : IOrderStore
{
    public async Task DeactivateAsync(int id, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var order = await db.Orders.FirstOrDefaultAsync(x => x.OrderId == id, ct)
            ?? throw new ApiException(404, "Pedido no encontrado.");
        var details = await db.OrderDetails.IgnoreQueryFilters()
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
