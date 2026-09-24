using System.ComponentModel.DataAnnotations;

namespace Finanzauto.Application.Identity;

public class PageQuery
{
    [Range(1, 1_000_000)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;

    [StringLength(100)]
    public string? Search { get; set; }
    public bool? Active { get; set; } = true;
}
