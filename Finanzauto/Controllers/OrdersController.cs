using Finanzauto.Application.Catalog;
using Finanzauto.Application.Orders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Finanzauto.Controllers;

[ApiController]
[Route("Orders")]
[Authorize]
public sealed class OrdersController(IOrderService orders) : ControllerBase
{
    private int ActorId => int.Parse(User.FindFirst("sub")!.Value, System.Globalization.CultureInfo.InvariantCulture);

    [HttpGet]
    public async Task<ActionResult<CatalogPage<OrderResponse>>> List([FromQuery] OrderQuery query, CancellationToken ct)
        => Ok(await orders.ListAsync(query, ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrderResponse>> Get(int id, CancellationToken ct)
        => Ok(await orders.GetAsync(id, ct));

    [HttpPost]
    public async Task<ActionResult<OrderResponse>> Create(OrderRequest request, CancellationToken ct)
        => Ok(await orders.CreateAsync(request, ActorId, ct));

    [HttpPut("{id:int}")]
    public async Task<ActionResult<OrderResponse>> Update(int id, OrderRequest request, CancellationToken ct)
        => Ok(await orders.UpdateAsync(id, request, ct));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await orders.DeleteAsync(id, ct);
        return NoContent();
    }

    [HttpDelete("{orderId:int}/Details/{detailId:int}")]
    public async Task<IActionResult> DeleteDetail(int orderId, int detailId, CancellationToken ct)
    {
        await orders.DeleteDetailAsync(orderId, detailId, ct);
        return NoContent();
    }
}
