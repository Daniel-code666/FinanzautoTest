namespace Finanzauto.Application.Catalog;

public class CategoryResponse
{
    public int Id { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool HasPicture { get; set; }
}
