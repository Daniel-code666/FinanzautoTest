using System.ComponentModel.DataAnnotations;
using Finanzauto.Application.Catalog;
using Finanzauto.Application.Partners;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Finanzauto.Controllers;

[ApiController]
[Route("Suppliers")]
[Authorize]
[RequestSizeLimit(8 * 1024 * 1024)]
public sealed class SuppliersController(IPartnerService partners) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<CatalogPage<SupplierDetailResponse>>> List([FromQuery] PartnerQuery query, CancellationToken ct)
    {
        var result = await partners.ListSuppliersAsync(query, ct);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SupplierDetailResponse>> Get(int id, CancellationToken ct)
    {
        var result = await partners.GetSupplierAsync(id, ct);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<SupplierDetailResponse>> Create(SupplierRequest request, CancellationToken ct)
    {
        var item = await partners.CreateSupplierAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = item.Id }, item);
    }

    [HttpPost("Bulk")]
    public async Task<ActionResult<BulkResponse<SupplierDetailResponse>>> CreateBulk([FromBody, Required, MinLength(1), MaxLength(1000)] SupplierRequest[] requests, CancellationToken ct)
    {
        var result = await partners.CreateSuppliersAsync(requests, ct);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<SupplierDetailResponse>> Update(int id, SupplierRequest request, CancellationToken ct)
    {
        var result = await partners.UpdateSupplierAsync(id, request, ct);
        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await partners.DeleteSupplierAsync(id, ct);
        return NoContent();
    }
}
