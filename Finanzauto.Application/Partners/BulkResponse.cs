using Finanzauto.Application.Catalog;

namespace Finanzauto.Application.Partners;

public class BulkResponse<T>
{
    public int CreatedCount { get; set; }
    public IReadOnlyList<T> Items { get; set; } = null!;
}
