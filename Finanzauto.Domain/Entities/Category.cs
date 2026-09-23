namespace Finanzauto.Domain.Entities;

public sealed class Category : Entity
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public byte[]? Picture { get; set; }
    public string? PictureContentType { get; set; }
    public ICollection<Product> Products { get; set; } = new List<Product>();
}

