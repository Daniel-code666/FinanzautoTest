namespace Finanzauto.Application.Catalog;

public class CategoryDetailResponse
{
    public int Id { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public byte[]? Picture { get; set; }
    public string? PictureContentType { get; set; }
}
