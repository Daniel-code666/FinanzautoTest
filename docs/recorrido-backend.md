# Recorrido de los mecanismos del backend

Esta guía explica las partes que requieren más contexto para seguir el código. Los constructores y el comportamiento público se conservan.

## Generación e inserción masiva

La petición `POST /Product` llega a `ProductGenerationService`, que coordina la generación y mide su duración. `RandomProductGenerator` produce los productos con `yield return`: cada producto se crea cuando el consumidor avanza en el recorrido, sin construir primero una lista de 100.000 objetos. Los precios se generan en centavos y luego se convierten a decimal. El operador `%` permite recorrer las categorías repetidamente y repartir los productos entre ellas.

`BulkProductWriter.WriteAsync` abre una transacción y valida las referencias con EF. `Chunk(1000)` divide el iterador en arreglos de hasta 1.000 productos. Cada arreglo se guarda con `AddRange` y `SaveChangesAsync`; después sus productos se separan del seguimiento de EF con `EntityState.Detached` para limitar el uso de memoria.

`CommitAsync` confirma la transacción al terminar todos los lotes. Si una fila falla o la operación se cancela, se revierten también los lotes guardados anteriormente. La petición HTTP espera a que termine este proceso. EF realiza más trabajo por producto que la carga binaria anterior; se prioriza un código más sencillo.

## Validación de referencias

`CatalogReferenceGuard.ValidateAsync` usa `CountAsync` para comprobar las categorías distintas y `AnyAsync` para comprobar el proveedor. Las consultas se ejecutan con EF y una referencia inexistente o inactiva produce HTTP 409. La misma validación se usa al guardar productos manualmente.

Estas consultas no bloquean las referencias: otra operación puede desactivarlas después de validarlas. Es una diferencia de concurrencia aceptada. Las claves foráneas siguen garantizando la integridad referencial, pero no el estado `Active`.

## Eliminación lógica

`EntityStatus.SetActive<T>(entity, active)`, en la capa de aplicación, asigna el estado de cualquier entidad que herede de `Entity`. Los servicios y repositorios usan el mismo método tanto para inactivar como para reactivar. La entidad debe estar siendo rastreada por EF al guardar. `SaveChanges` y `SaveChangesAsync` no están sobrescritos y únicamente persisten los cambios.

Los filtros globales ocultan los registros inactivos en las consultas habituales. `IgnoreQueryFilters` permite consultarlos para reactivación. `OrderStore.DeactivateAsync` consulta el pedido y sus detalles activos, aplica `SetActive` a todos y guarda en una transacción. No ofrece reactivación de órdenes ni existe una cascada lógica general para todas las relaciones.

`Remove` ya no se transforma en inactivación; produce un borrado físico al guardar. La aplicación usa `SetActive`. Los borrados físicos conservan las cascadas de PostgreSQL.

## Normalización del correo

`EmailNormalizer.SetEmail` asigna el correo sin espacios exteriores y su versión normalizada en mayúsculas. Se usa en registro, creación administrativa y modificaciones de usuario o perfil. `EmailNormalizer.Normalize` prepara búsquedas de login, recuperación de contraseña, validación de duplicados e inicialización del administrador. Guardar directamente con el contexto ya no normaliza correos automáticamente.

## Fechas de auditoría

`AuditTable` es una clase base, no una tabla independiente ni un historial de cambios. Sus propiedades se almacenan en cada tabla concreta.

La migración `AuditDatesWithoutSecurityStamp` define los triggers que asignan `CreationDate` al insertar y dejan `UpdatedDate` inicialmente en null. Al actualizar, conservan la fecha de creación y cambian la de actualización solamente si cambian datos de la fila. La comparación excluye las propias fechas y las columnas calculadas `SearchName` y `NormalizedName`.

Al hacerlo en PostgreSQL, las fechas también funcionan con EF y SQL ejecutado desde DBeaver. `ConfigureAuditDates` indica a EF que estos valores los genera la base de datos y que debe ignorar los valores asignados desde la aplicación. Esta aclaración del código no requiere nuevas migraciones.

## Autenticación JWT

El login valida las credenciales y el estado del empleado y su rol. `JwtTokenService` crea un token con el identificador del empleado (`sub`), un identificador del token (`jti`), el rol, el emisor, el destinatario y su vigencia. La firma HMAC SHA-256 permite detectar alteraciones; no cifra el contenido del token.

`AuthenticationRegistration` divide la configuración en tres pasos: validar los ajustes, establecer las comprobaciones del token y consultar la sesión actual después de validar la firma y la vigencia. Esta última consulta comprueba que el empleado y su rol sigan activos y que el nombre del rol coincida con el token.

Desactivar al empleado o su rol hace que la sesión se rechace con HTTP 401. Reactivarlo con el mismo rol puede permitir de nuevo un token aún vigente. No hay `SecurityStamp` ni revocación de tokens por cambio de contraseña.

Se conserva el acceso definido para la prueba: `/Profile/Password` requiere autenticación y contraseña actual; `/UserAdministration/Users/ResetPassword` permite acceso anónimo, con identificación por correo o id.

## Verificación

Con Docker en ejecución, ejecutar desde la raíz:

```powershell
dotnet test Finanzauto.slnx -c Release
```

Las pruebas incluyen la inserción de 100.000 productos, reversión de lotes fallidos, auditoría con SQL directo, cascadas físicas, eliminación lógica de pedidos con guardado síncrono y asíncrono, rechazo de tokens alterados y contratos de respuesta del API.
