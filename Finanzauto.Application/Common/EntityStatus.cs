using Finanzauto.Domain.Entities;

namespace Finanzauto.Application.Common;

public static class EntityStatus
{
    // Cambia el estado; el repositorio guarda después todas las entidades afectadas.
    public static void SetActive<T>(T entity, bool active) where T : Entity
    {
        entity.Active = active;
    }
}
