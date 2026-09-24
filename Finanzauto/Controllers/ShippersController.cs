using Finanzauto.Application.Catalog;
using Finanzauto.Application.Shippers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Finanzauto.Controllers;

[ApiController]
[Route("Shippers")]
[Authorize]
public sealed class ShippersController(IShipperService shippers) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<CatalogPage<ShipperResponse>>> List([FromQuery] CatalogQuery query, CancellationToken ct)
        => Ok(await shippers.ListAsync(query, ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ShipperResponse>> Get(int id, CancellationToken ct)
        => Ok(await shippers.GetAsync(id, ct));

    [HttpPost]
    public async Task<ActionResult<ShipperResponse>> Create(ShipperRequest request, CancellationToken ct)
        => Ok(await shippers.CreateAsync(request, ct));

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ShipperResponse>> Update(int id, ShipperRequest request, CancellationToken ct)
        => Ok(await shippers.UpdateAsync(id, request, ct));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await shippers.DeleteAsync(id, ct);
        return NoContent();
    }
}
