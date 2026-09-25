using System.ComponentModel.DataAnnotations;
using Finanzauto.Application.Catalog;

namespace Finanzauto.Application.Orders;

public sealed class OrderQuery : CatalogQuery, IValidatableObject
{
    [Range(1, int.MaxValue)]
    public int? CustomerId { get; set; }
    [Range(1, int.MaxValue)]
    public int? EmployeeId { get; set; }
    [Range(1, int.MaxValue)]
    public int? ShipVia { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if ((FromDate.HasValue && FromDate.Value.Kind != DateTimeKind.Utc) ||
            (ToDate.HasValue && ToDate.Value.Kind != DateTimeKind.Utc))
            yield return new ValidationResult("Los filtros de fecha deben enviarse en UTC.");
        if (FromDate > ToDate)
            yield return new ValidationResult("FromDate no puede ser posterior a ToDate.");
    }
}
