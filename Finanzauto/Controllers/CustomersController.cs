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
        => Ok(await partners.ListCustomersAsync(query, ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CustomerResponse>> Get(int id, CancellationToken ct)
        => Ok(await partners.GetCustomerAsync(id, ct));

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<CustomerResponse>> Create(CustomerRequest request, CancellationToken ct)
        => Ok(await partners.CreateCustomerAsync(request, ct));

    [HttpPost("Bulk")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<BulkResponse<CustomerResponse>>> CreateBulk([FromBody, Required, MinLength(1), MaxLength(1000)] CustomerRequest[] requests, CancellationToken ct)
        => Ok(await partners.CreateCustomersAsync(requests, ct));

    [HttpPut("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<CustomerResponse>> Update(int id, ContactRequest request, CancellationToken ct)
        => Ok(await partners.UpdateCustomerAsync(id, request, ct));

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await partners.DeleteCustomerAsync(id, ct);
        return NoContent();
    }
}
