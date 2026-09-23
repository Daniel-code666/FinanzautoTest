using System.Diagnostics;

namespace Finanzauto.Application.Catalog;

public sealed class ProductGenerationService(IRandomProductGenerator generator, IBulkProductWriter writer)
    : IProductGenerationService
{
    public async Task<GenerationResponse> GenerateAsync(GenerateProductsRequest request, CancellationToken ct)
    {
        var generationId = Guid.NewGuid();
        var watch = Stopwatch.StartNew();
        var count = await writer.WriteAsync(generator.Generate(request, generationId),
            request.CategoryIds, request.SupplierId, ct);
        return new(generationId, count, request.CategoryIds, request.SupplierId, watch.ElapsedMilliseconds);
    }
}

