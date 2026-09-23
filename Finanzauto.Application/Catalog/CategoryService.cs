using Finanzauto.Application.Common.Exceptions;
using Finanzauto.Domain.Entities;

namespace Finanzauto.Application.Catalog;

public sealed class CategoryService(ICatalogStore store) : ICategoryService
{
    public Task<CatalogPage<CategoryResponse>> ListAsync(CatalogQuery query, CancellationToken ct) =>
        store.ListCategoriesAsync(query, ct);

    public async Task<CategoryDetailResponse> GetAsync(int id, CancellationToken ct) =>
        await store.GetCategoryAsync(id, ct) ?? throw new ApiException(404, "Categoría no encontrada.");

    public async Task<CategoryDetailResponse> CreateAsync(CategoryRequest request, CancellationToken ct)
    {
        var id = await store.SaveCategoryAsync(null, Map(request), ct);
        return await GetAsync(id, ct);
    }

    public async Task<CategoryDetailResponse> UpdateAsync(int id, CategoryRequest request, CancellationToken ct)
    {
        await store.SaveCategoryAsync(id, Map(request), ct);
        return await GetAsync(id, ct);
    }

    public Task DeleteAsync(int id, CancellationToken ct) => store.DeactivateCategoryAsync(id, ct);

    private static Category Map(CategoryRequest request) => new()
    {
        CategoryName = request.CategoryName.Trim(),
        Description = request.Description?.Trim(),
        Picture = request.Picture,
        PictureContentType = GetPictureType(request.Picture)
    };

    private static string? GetPictureType(byte[]? bytes)
    {
        if (bytes is null) return null;
        if (bytes.Length > 2 * 1024 * 1024)
            throw new ApiException(400, "La imagen no puede superar 2 MiB.");
        if (bytes.Length >= 8 && bytes.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }))
            return "image/png";
        if (bytes.Length >= 3 && bytes[0] == 255 && bytes[1] == 216 && bytes[2] == 255)
            return "image/jpeg";
        if (bytes.Length >= 12 && bytes.AsSpan(0, 4).SequenceEqual("RIFF"u8) && bytes.AsSpan(8, 4).SequenceEqual("WEBP"u8))
            return "image/webp";
        throw new ApiException(400, "Picture debe ser una imagen PNG, JPEG o WebP codificada en base64.");
    }
}

