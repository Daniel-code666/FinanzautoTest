using Finanzauto.Application.Catalog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Finanzauto.Controllers;

[ApiController]
[Authorize]
[RequestSizeLimit(3 * 1024 * 1024)]
public sealed class CategoryController(ICategoryService categories) : ControllerBase
{
    [HttpPost("Category")]
    public async Task<ActionResult<CategoryDetailResponse>> Create(CategoryRequest request, CancellationToken ct)
    {
        var category = await categories.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = category.Id }, category);
    }

    [HttpGet("Categories")]
    public async Task<ActionResult<CatalogPage<CategoryResponse>>> List([FromQuery] CatalogQuery query, CancellationToken ct) =>
        Ok(await categories.ListAsync(query, ct));

    [HttpGet("Categories/{id:int}")]
    public async Task<ActionResult<CategoryDetailResponse>> Get(int id, CancellationToken ct) =>
        Ok(await categories.GetAsync(id, ct));

    [HttpPut("Categories/{id:int}")]
    public async Task<ActionResult<CategoryDetailResponse>> Update(int id, CategoryRequest request, CancellationToken ct) =>
        Ok(await categories.UpdateAsync(id, request, ct));

    [HttpDelete("Categories/{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await categories.DeleteAsync(id, ct);
        return NoContent();
    }
}
