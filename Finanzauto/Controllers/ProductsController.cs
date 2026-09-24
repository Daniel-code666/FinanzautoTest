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
    {
        var result = await products.ListAsync(query, ct);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductDetailResponse>> Get(int id, CancellationToken ct)
    {
        var result = await products.GetAsync(id, ct);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<ProductDetailResponse>> Create(ProductRequest request, CancellationToken ct)
    {
        var product = await products.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = product.Product.Id }, product);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ProductDetailResponse>> Update(int id, ProductRequest request, CancellationToken ct)
    {
        var result = await products.UpdateAsync(id, request, ct);
        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await products.DeleteAsync(id, ct);
        return NoContent();
    }
}
