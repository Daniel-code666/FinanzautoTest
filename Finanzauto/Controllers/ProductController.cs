using Finanzauto.Application.Catalog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Finanzauto.Controllers;

[ApiController]
[Route("Product")]
[Authorize]
public sealed class ProductController(IProductGenerationService generation) : ControllerBase
{
    /// <summary>
    /// Genera productos de manera masiva
    /// </summary>
    /// <param name="request"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    [HttpPost]
    [EnableRateLimiting("ProductGeneration")]
    public async Task<ActionResult<GenerationResponse>> Generate(GenerateProductsRequest request, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await generation.GenerateAsync(request, ct));
}
