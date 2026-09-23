namespace Finanzauto.Domain.Entities;

/// <summary>Fechas UTC administradas por PostgreSQL para todas las entidades.</summary>
public abstract class AuditTable
{
    public DateTime CreationDate { get; private set; }
    public DateTime? UpdatedDate { get; private set; }
}
