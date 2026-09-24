namespace Finanzauto.Application.Catalog;

public class GenerationResponse
{
    public Guid GenerationId { get; set; }
    public int CreatedCount { get; set; }
    public int[] CategoryIds { get; set; } = null!;
    public int SupplierId { get; set; }
    public long ElapsedMilliseconds { get; set; }
}
