using Finanzauto.Application.Catalog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Finanzauto.Controllers;

[ApiController]
[Route("Products")]
[Authorize]
public sealed class ProductsController(IProductService products) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<CatalogPage<ProductResponse>>> List([FromQuery] ProductQuery query, CancellationToken ct)
        => Ok(await products.ListAsync(query, ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductDetailResponse>> Get(int id, CancellationToken ct)
        => Ok(await products.GetAsync(id, ct));

    [HttpPost]
    public async Task<ActionResult<ProductDetailResponse>> Create(ProductRequest request, CancellationToken ct)
        => Ok(await products.CreateAsync(request, ct));

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ProductDetailResponse>> Update(int id, ProductRequest request, CancellationToken ct)
        => Ok(await products.UpdateAsync(id, request, ct));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await products.DeleteAsync(id, ct);
        return NoContent();
    }
}
