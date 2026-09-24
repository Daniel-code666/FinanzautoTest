namespace Finanzauto.Application.Identity;

public class PageResult<T>
{
    public IReadOnlyList<T> Items { get; set; } = null!;
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
