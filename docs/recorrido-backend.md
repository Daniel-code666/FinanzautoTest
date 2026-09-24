# Recorrido de los mecanismos del backend

Esta guía explica las partes que requieren más contexto para seguir el código. Los constructores y el comportamiento público se conservan.

## Generación e inserción masiva

La petición `POST /Product` llega a `ProductGenerationService`, que coordina la generación y mide su duración. `RandomProductGenerator` produce los productos con `yield return`: cada producto se crea cuando el consumidor avanza en el recorrido, sin construir primero una lista de 100.000 objetos. Los precios se generan en centavos y luego se convierten a decimal. El operador `%` permite recorrer las categorías repetidamente y repartir los productos entre ellas.

`BulkProductWriter.WriteAsync` abre una transacción, valida y bloquea las referencias, envía los productos y confirma el resultado. `CopyProductsAsync` usa el protocolo binario `COPY` de PostgreSQL para enviar filas sin añadir cada producto al seguimiento de cambios de EF. El orden y los tipos de los valores enviados deben coincidir con las columnas de la instrucción `COPY`.

`CompleteAsync` termina el envío; `CommitAsync` confirma la transacción. Si una fila falla, la transacción se desecha sin confirmar y se revierte el lote. La petición HTTP espera a que termine este proceso: no es una tarea en segundo plano.

## Referencias y bloqueos

`CatalogReferenceGuard` consulta categorías y proveedor activos mediante `FOR SHARE`, dentro de la transacción del consumidor. Esto impide que otra transacción modifique o elimine esas referencias mientras se guardan los productos. Las lecturas normales pueden continuar; las operaciones incompatibles esperan a que termine la transacción.

Las categorías se consultan en un orden estable para reducir el riesgo de bloqueos mutuos. Si falta una referencia activa, se devuelve un conflicto HTTP 409. Las consultas interpoladas de EF envían los valores como parámetros.

## Eliminación lógica

`FinanzautoDbContext` prepara los cambios tanto en `SaveChanges` como en `SaveChangesAsync`. Primero identifica pedidos eliminados o desactivados, carga sus detalles activos aunque no estuvieran cargados y los desactiva. Después convierte las eliminaciones de entidades en actualizaciones de `Active = false` y normaliza los correos de empleados.

Los filtros globales ocultan los registros inactivos en las consultas habituales. `IgnoreQueryFilters` permite consultarlos cuando una operación lo necesita. La desactivación de detalles del pedido es explícita; no existe una cascada lógica general para todas las relaciones.

Un `DELETE` ejecutado directamente en PostgreSQL no pasa por `SaveChanges`: usa las cascadas físicas definidas en las relaciones de la base de datos.

## Fechas de auditoría

`AuditTable` es una clase base, no una tabla independiente ni un historial de cambios. Sus propiedades se almacenan en cada tabla concreta.

La migración `AuditDatesWithoutSecurityStamp` define los triggers que asignan `CreationDate` al insertar y dejan `UpdatedDate` inicialmente en null. Al actualizar, conservan la fecha de creación y cambian la de actualización solamente si cambian datos de la fila. La comparación excluye las propias fechas y las columnas calculadas `SearchName` y `NormalizedName`.

Al hacerlo en PostgreSQL, las fechas también funcionan con `COPY` y SQL ejecutado desde DBeaver. `ConfigureAuditDates` indica a EF que estos valores los genera la base de datos y que debe ignorar los valores asignados desde la aplicación. Esta aclaración del código no requiere nuevas migraciones.

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
