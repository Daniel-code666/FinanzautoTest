namespace Finanzauto.Application.Catalog;

public class ProductDetailResponse
{
    public ProductResponse Product { get; set; } = null!;
    public CategoryDetailResponse Category { get; set; } = null!;
}
