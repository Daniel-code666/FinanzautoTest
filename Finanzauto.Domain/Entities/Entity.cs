namespace Finanzauto.Domain.Entities;

public abstract class Entity : AuditTable
{
    public bool Active { get; set; } = true;
}
