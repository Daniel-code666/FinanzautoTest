using System.ComponentModel.DataAnnotations;
using Finanzauto.Application.Catalog;
using Finanzauto.Application.Partners;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Finanzauto.Controllers;

[ApiController]
[Route("Customers")]
[Authorize]
[RequestSizeLimit(8 * 1024 * 1024)]
public sealed class CustomersController(IPartnerService partners) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<CatalogPage<CustomerResponse>>> List([FromQuery] PartnerQuery query, CancellationToken ct)
    {
        var result = await partners.ListCustomersAsync(query, ct);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CustomerResponse>> Get(string id, CancellationToken ct)
    {
        var result = await partners.GetCustomerAsync(id, ct);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<CustomerResponse>> Create(CustomerRequest request, CancellationToken ct)
    {
        var item = await partners.CreateCustomerAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = item.Id }, item);
    }

    [HttpPost("Bulk")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<BulkResponse<CustomerResponse>>> CreateBulk([FromBody, Required, MinLength(1), MaxLength(1000)] CustomerRequest[] requests, CancellationToken ct)
    {
        var result = await partners.CreateCustomersAsync(requests, ct);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<CustomerResponse>> Update(string id, ContactRequest request, CancellationToken ct)
    {
        var result = await partners.UpdateCustomerAsync(id, request, ct);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        await partners.DeleteCustomerAsync(id, ct);
        return NoContent();
    }
}
