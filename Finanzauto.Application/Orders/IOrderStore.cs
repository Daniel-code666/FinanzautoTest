namespace Finanzauto.Application.Orders;

public interface IOrderStore
{
    Task DeactivateAsync(int id, CancellationToken ct);
}
