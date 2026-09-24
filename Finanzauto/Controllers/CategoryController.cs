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
    public async Task<ActionResult<CatalogPage<CategoryResponse>>> List([FromQuery] CatalogQuery query, CancellationToken ct)
    {
        var result = await categories.ListAsync(query, ct);
        return Ok(result);
    }

    [HttpGet("Categories/{id:int}")]
    public async Task<ActionResult<CategoryDetailResponse>> Get(int id, CancellationToken ct)
    {
        var result = await categories.GetAsync(id, ct);
        return Ok(result);
    }

    [HttpPut("Categories/{id:int}")]
    public async Task<ActionResult<CategoryDetailResponse>> Update(int id, CategoryRequest request, CancellationToken ct)
    {
        var result = await categories.UpdateAsync(id, request, ct);
        return Ok(result);
    }

    [HttpDelete("Categories/{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await categories.DeleteAsync(id, ct);
        return NoContent();
    }
}
