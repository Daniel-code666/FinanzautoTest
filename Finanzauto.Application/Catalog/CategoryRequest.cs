using System.ComponentModel.DataAnnotations;

namespace Finanzauto.Application.Catalog;

public sealed class CategoryRequest
{
    [Required, StringLength(100)]
    public string CategoryName { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    [MaxLength(2 * 1024 * 1024)]
    public byte[]? Picture { get; set; }
}
