# Finanzauto — backend

Base del backend de la prueba técnica ASISYA, en .NET 10, EF Core 10 y PostgreSQL.

## Estado

Implementados: estructura por capas, nueve entidades, relaciones, restricciones,
filtros de eliminación lógica, migración inicial, roles Admin/User y Docker Compose.
La API incluye registro, login JWT y administración de usuarios y roles, con DTOs,
validaciones y autorización por rol. También expone `GET /health` y OpenAPI en desarrollo.
También incluye CRUD de productos y categorías, consultas paginadas, foto de
categoría en el detalle y generación masiva de hasta 100.000 productos.
Incluye pruebas unitarias xUnit con Moq y pruebas de integración con PostgreSQL
temporal, ejecutadas automáticamente por GitHub Actions.

## Arquitectura

Los DTOs se organizan en archivos individuales dentro de Catalog, Identity y
Partners. Las respuestas usan clases con propiedades y se crean mediante
asignaciones explícitas. Los controladores y servicios emplean métodos con bloques
y variables intermedias para facilitar su lectura; conservan los constructores
primarios para inyección de dependencias. El refactor de estilo mantiene nombres
JSON, rutas, validaciones, permisos y consultas en base de datos.

Los mapeos de clientes y proveedores cargados en memoria son métodos normales en
`PartnerMapping`. Los listados construyen el DTO directamente dentro de `Select`
en el repositorio, para que EF seleccione las columnas y aplique filtros, orden y
paginación en PostgreSQL. Las asignaciones se repiten de forma explícita entre
ambos caminos; las pruebas comparan los resultados de listado y detalle para
evitar diferencias. El orden de productos usa un `switch` convencional y mantiene
el ID como desempate para nombres o precios iguales.

- `Finanzauto.Domain`: entidades sin dependencias de EF ni de ASP.NET.
- `Finanzauto.Application`: casos de uso de identidad y catálogo, DTOs e interfaces.
  Depende únicamente de Domain.
- `Finanzauto.Infrastructure`: DbContext, configuraciones por entidad y migraciones.
  Depende de Application y registra su implementación mediante AddInfrastructure.
- `Finanzauto`: API existente, punto de composición de la inyección de dependencias.

Se mantiene el nombre del proyecto API para conservar la configuración existente
de Visual Studio. El DbContext tiene duración scoped. No se agregan repositorios
genéricos ni interfaces vacías anticipadamente: los contratos se definirán según
los casos de uso y los principios SOLID.

## Ejecutar con Docker

Requisitos: Docker Desktop con contenedores Linux y Docker Compose.
Desde la carpeta que contiene `Finanzauto.slnx`:

```powershell
Copy-Item .env.example .env
docker compose up --build -d
docker compose ps
```

El archivo .env es local y está excluido de Git. La contraseña del ejemplo es
exclusivamente de desarrollo. La API aplica las migraciones al iniciar porque
Compose activa `Database__ApplyMigrations`. PostgreSQL conserva los datos en un
volumen y la API espera a que la base esté disponible.

Postman: `GET http://localhost:8080/health`.
Respuesta esperada cuando hay conexión: `{"status":"Healthy"}`.
OpenAPI: `http://localhost:8080/openapi/v1.json`.
Swagger UI: `http://localhost:8080/swagger` (solo en Development).
Ejecutar POST /Login, copiar accessToken y pegarlo en **Authorize**, sin el prefijo
Bearer. Swagger agregará el encabezado Authorization a las operaciones protegidas.
Login, registro y health permanecen públicos. Los endpoints administrativos
requieren Admin para modificar otros usuarios y para toda la gestión de roles.
Los GET de usuarios están disponibles para cualquier usuario autenticado.
El token no se persiste entre recargas.

DBeaver:

| Campo | Valor |
|---|---|
| Driver | PostgreSQL |
| Host | localhost |
| Puerto | 5432 |
| Base | finanzauto |
| Usuario | finanzauto |
| Contraseña | POSTGRES_PASSWORD de .env |

Los puertos se publican solo en la interfaz local. La API usa `db:5432` dentro
de Docker. `docker compose down` detiene los servicios conservando el volumen.

## Desarrollo local con .NET

Requiere SDK .NET 10. Abrir la solución raíz Finanzauto.slnx.

```powershell
dotnet restore Finanzauto.slnx
dotnet build Finanzauto.slnx
dotnet tool restore
docker compose up -d db
$env:ConnectionStrings__Finanzauto = 'Host=localhost;Port=5432;Database=finanzauto;Username=finanzauto;Password=FinanzautoLocal2026'
$env:Jwt__SigningKey = '<copiar JWT_SIGNING_KEY de .env>'
dotnet ef database update --project Finanzauto.Infrastructure
dotnet run --project Finanzauto --no-launch-profile --urls http://localhost:8080
```

Adaptar la contraseña a .env. Si la API Docker ocupa 8080, detener únicamente ese
servicio con `docker compose stop api` o elegir otro puerto local.
No se guardan credenciales en appsettings.

Para una nueva migración:

```powershell
dotnet ef migrations add NombreDelCambio --project Finanzauto.Infrastructure --output-dir Persistence/Migrations
```

La fábrica de diseño permite generar migraciones sin una base encendida.
En producción las migraciones deben ejecutarse como una tarea de despliegue,
sin activarlas simultáneamente al arrancar múltiples instancias.

## Modelo y decisiones

- Category, Supplier, Product, Customer, Employee, Shipper, Order, OrderDetail y Role.
- Employee es la identidad de acceso: Email, NormalizedEmail, PasswordHash y RoleId.
  El índice único de NormalizedEmail incluye empleados inactivos. EmailNormalizer
  normaliza el correo en los servicios de aplicación al crear o editar usuarios.
  Los roles se crean con migraciones y el administrador se
  inicializa opcionalmente con las variables BootstrapAdmin.
- Category y Supplier son obligatorios en Product; Customer y Employee en Order.
  ShipVia y ReportsTo son opcionales. ReportsTo referencia al supervisor.
- CustomerId y las demás claves simples son enteros autoincrementales.
  OrderDetail usa OrderDetailId autoincremental y un índice único sobre
  OrderId + ProductId únicamente para filas activas.
- Dinero: decimal(18,2). Discount: fracción de 0 a 1. Precios, existencias y flete
  no negativos; cantidades de detalle positivas.
- Fechas de pedidos: timestamp with time zone, usando UTC desde la aplicación.
  BirthDate y HireDate: date.
- Picture y Photo: bytea. Category incluye PictureContentType.
- Discontinued es un estado comercial distinto de Active.

## Eliminación

Todas las entidades incluyen Active con valor inicial true y filtro global de EF.
`EntityStatus.SetActive(entity, active)` es el mecanismo compartido para activar
e inactivar entidades. Los repositorios y servicios lo llaman después de validar
la operación y guardan con los métodos normales de EF, sin sobrescribir SaveChanges.
OrderStore inactiva el pedido y sus detalles con el mismo mecanismo dentro de una
transacción. No ofrece reactivación de órdenes. Reactivar usuarios o roles no
reactiva dependientes automáticamente.

Todas las claves foráneas usan ON DELETE CASCADE para borrados físicos directos
desde DBeaver: la cascada va del principal hacia sus dependientes, nunca a la inversa.
Incluye Role → Employee, Employee → subordinados/pedidos y Product → OrderDetail.
Eliminar una categoría físicamente puede eliminar detalles de pedidos existentes.

Los filtros globales se pueden omitir explícitamente con IgnoreQueryFilters para
operaciones internas que necesiten históricos. Al consultar históricos con relaciones
obligatorias también se deben considerar los filtros del principal.
Remove, ExecuteDelete y DELETE SQL eliminan físicamente: no deben usarse para
las inactivaciones habituales de la aplicación.

La API impide desactivar categorías con productos activos y solo acepta categorías
y proveedores activos al crear/editar/generar productos. Las validaciones usan
consultas normales; se acepta una posible desactivación concurrente posterior.
Los proveedores y clientes tienen CRUD e inserción masiva. Un proveedor con productos
activos no se puede desactivar, ni un cliente con pedidos activos.
El proveedor inicial tiene identificador 1.
Las claves foráneas por sí solas comprueban existencia, no el valor de Active.

## Validación del backend

Las pruebas automatizadas y el pipeline CI están implementados. La ejecución
del frontend mediante Compose se describe al final de este documento.

## Login y administración de usuarios

Configurar `JWT_SIGNING_KEY` en .env con un secreto aleatorio de al menos 32 bytes.
Para crear el primer administrador, configurar `BOOTSTRAP_ADMIN_EMAIL` y
`BOOTSTRAP_ADMIN_PASSWORD` (12-128 caracteres). No se imprime su contraseña.
Si la cuenta ya existe como administrador, el arranque no cambia su contraseña
ni la reactiva. Si pertenece a otro rol, el arranque rechaza esa configuración.
Después de inicializar la cuenta se pueden retirar las dos variables BootstrapAdmin.
Cambiar estas variables posteriormente no cambia la contraseña de una cuenta existente.

| Método | Ruta | Acceso |
|---|---|---|
| POST | /Login | Público |
| POST | /UserAdministration/Register | Público, siempre crea rol User |
| GET | /UserAdministration/Users | JWT |
| POST | /UserAdministration/Users | Admin |
| GET | /UserAdministration/Users/{id} | JWT |
| PUT / DELETE | /UserAdministration/Users/{id} | Admin |
| POST | /UserAdministration/Users/{id}/Reactivate | Admin |
| PUT | /UserAdministration/Users/ResetPassword | Público |
| GET | /UserAdministration/Roles | Admin |
| POST | /UserAdministration/Roles | Admin |
| GET | /UserAdministration/Roles/{id} | Admin |
| PUT / DELETE | /UserAdministration/Roles/{id} | Admin |
| POST | /UserAdministration/Roles/{id}/Reactivate | Admin |

Registro:

```json
{
  "firstName": "Ana",
  "lastName": "Pérez",
  "email": "ana@example.com",
  "password": "EjemploLocal2026!"
}
```

Login:

```json
{
  "email": "ana@example.com",
  "password": "EjemploLocal2026!"
}
```

La respuesta contiene accessToken, tokenType, expiresAtUtc y user. En Postman,
seleccionar Authorization → Bearer Token y pegar accessToken.
No se devuelven PasswordHash ni entidades directamente.

Para crear un usuario como Admin, usar el cuerpo de registro más `roleId`.
Para editar: firstName, lastName, email y roleId; el cambio de contraseña tiene
su propio endpoint, con `{"email":"ana@example.com","password":"NuevaClaveLocal2026!"}`.
ResetPassword permite acceso anónimo para esta prueba técnica, sin JWT ni contraseña
actual. Recibe opcionalmente id por query; sin id busca por correo normalizado.
Para crear o renombrar un rol: `{"name":"Operador"}`.
Los roles personalizados pueden consultar usuarios y Customers, administrar su propio
perfil y operar Suppliers/productos/categorías; no pueden gestionar roles ni otros usuarios.

Listados: `?page=1&pageSize=20&search=ana&active=true`; Users permite además
`roleId=2`. PageSize admite 1-100, Page 1-1000000; el orden es por identificador.
Active es true por defecto, false permite ver eliminados y `active=` incluye
ambos estados. La respuesta contiene items, totalCount, page y pageSize.
El filtrado, conteo y paginación se ejecutan en PostgreSQL.

DELETE siempre desactiva. Reactivate es explícito y exige un rol activo para el
usuario. No se permite desactivar la propia cuenta ni cambiar el propio rol.
Los roles base Admin y User no se pueden renombrar ni desactivar desde la API;
esto conserva la política administrativa y el rol del registro público.
Nombres de rol y correos son únicos sin distinguir mayúsculas, también entre
registros inactivos.

JWT valida firma HS256, emisor, audiencia y expiración (60 minutos por defecto,
configurable con Jwt__ExpirationMinutes entre 1 y 1440). Cada petición autenticada
consulta el estado actual del empleado y rol y comprueba que el nombre del rol
coincida con el JWT. No se utiliza SecurityStamp: editar datos o cambiar la contraseña
no revoca tokens emitidos. Desactivar un empleado/rol bloquea el acceso mientras
permanezca inactivo; si se reactiva, un token no vencido puede volver a funcionar.
Cambiar el rol rechaza tokens cuyo rol ya no coincida. No hay refresh tokens.

Las contraseñas se almacenan con PasswordHasher de ASP.NET Core Identity.
Login y registro comparten un límite de 10 solicitudes por minuto por IP (429
al superarlo); el contador es local a cada instancia. Para escalar se debe
centralizar este límite en el gateway o un almacén compartido. Un proxy requiere
configurar explícitamente sus direcciones de confianza antes de usar forwarded headers.
El Compose local usa HTTP en loopback; un despliegue externo requiere HTTPS.

Errores: 400 validación, 401 credenciales/token inválidos, 403 falta de permisos,
404 registro inexistente y 409 duplicados o conflictos de operación.
La política predeterminada exige autenticación para nuevos endpoints. Solo se
declaran anónimos login, registro, health y OpenAPI de desarrollo.

## Manejo global de errores

`ExceptionHandlingMiddleware` es el punto único de captura de excepciones durante
las peticiones HTTP, registrado antes de rate limiting, autenticación, autorización
y controladores. Reemplaza el manejador específico de identidad.
Los errores usan el contrato `ApiErrorResponse`:

```json
{
  "description": "El correo ya está registrado.",
  "exception": "IdentityException",
  "httpCode": 409
}
```

`exception` contiene el nombre del tipo, no el objeto Exception serializado.
La descripción de excepciones controladas corresponde a Message. Para nuevos
módulos se debe usar o extender `ApiException`, indicando un código entre 400 y 599
y un mensaje apto para el cliente. IdentityException hereda de esta base.
No es necesario capturar estas excepciones en cada controlador.

Las excepciones inesperadas devuelven 500: en desarrollo se muestra su mensaje;
en producción se devuelve una descripción genérica. El servidor registra la
excepción completa y el TraceId para diagnóstico, sin enviar stack traces ni
excepciones internas al cliente.

La validación de modelos usa el mismo objeto con exception=ValidationException
y código 400. Los errores HTTP sin cuerpo (401, 403, 404, 405, 415, 429, etc.)
se completan con exception=HttpError, conservando sus encabezados HTTP.
Una desconexión del cliente no se convierte en 500. Si la respuesta ya comenzó,
el middleware no intenta reemplazarla. Las excepciones del arranque (configuración,
migraciones, inicialización) ocurren antes del pipeline y se registran en el host.

## Catálogo: productos y categorías

Todos los endpoints de esta sección requieren JWT. Cualquier usuario autenticado
con empleado y rol activos puede consultar, crear, editar, eliminar y generar
productos, así como gestionar categorías y proveedores.

| Método | Ruta | Función |
|---|---|---|
| POST | /Category | Crear categoría |
| GET | /Categories | Categorías activas con búsqueda y paginación |
| GET / PUT / DELETE | /Categories/{id} | Detalle, edición o eliminación lógica |
| GET | /Suppliers | Proveedores activos con búsqueda y paginación |
| POST | /Products | Crear un producto manualmente |
| GET | /Products | Listado filtrado, paginado y ordenado |
| GET / PUT / DELETE | /Products/{id} | Detalle, edición o eliminación lógica |
| POST | /Product | Generar y guardar productos aleatorios |

Se conserva POST /Product en singular para la carga requerida por el enunciado;
la creación manual es POST /Products. La generación usa un servicio separado.
Las categorías SERVIDORES y CLOUD se crean por API, no mediante seed.

Crear una categoría:

```json
{
  "categoryName": "SERVIDORES",
  "description": "Infraestructura de servidores",
  "picture": null
}
```

Picture admite base64 PNG, JPEG o WebP de hasta 2 MiB. Se verifica la firma del
archivo y se infiere PictureContentType. PUT reemplaza los datos de la categoría:
picture=null elimina su foto. Los listados no descargan imágenes; el detalle de
producto devuelve `{ "product": {...}, "category": {...} }`, incluyendo picture
en base64 y pictureContentType. Los nombres de categoría son únicos sin distinguir
mayúsculas, incluso si el registro está inactivo.

Consultar GET /Suppliers y GET /Categories para obtener los identificadores.
Crear o editar un producto:

```json
{
  "productName": "Servidor de aplicaciones",
  "categoryId": 1,
  "supplierId": 1,
  "quantityPerUnit": "1 unidad",
  "unitPrice": 1500.50,
  "unitsInStock": 10,
  "unitsOnOrder": 0,
  "reorderLevel": 2,
  "discontinued": false
}
```

Precios no negativos con máximo dos decimales; existencias, unidades pedidas y nivel
de reposición no negativos. PUT reemplaza estos campos. DELETE cambia Active a
false y el producto deja de aparecer en listados y detalle, sin borrar históricos.

Filtros de GET /Products:

- page (1-1000000), pageSize (1-100, predeterminado 20).
- search: coincidencia parcial del nombre sin distinguir mayúsculas.
- categoryId, supplierId.
- minPrice y maxPrice, ambos inclusivos.
- inStock=true para existencias positivas; false para cero.
- discontinued=true/false.
- sortBy=Id, Name o Price; descending=true/false. Desempate estable por ProductId.

Ejemplo:
`/Products?categoryId=1&minPrice=100&maxPrice=5000&inStock=true&search=servidor&page=1&pageSize=25&sortBy=Price&descending=true`.
La respuesta contiene items, totalCount, page y pageSize. Categories y Suppliers
aceptan page, pageSize y search, y se ordenan por identificador.

## Carga masiva y rendimiento

Después de crear SERVIDORES y CLOUD, ejecutar POST /Product usando sus ids:

```json
{
  "count": 100000,
  "categoryIds": [1, 2],
  "supplierId": 1,
  "namePrefix": "Producto",
  "minPrice": 1,
  "maxPrice": 10000
}
```

Count admite 1-100000; categoryIds, de 1 a 100 ids positivos distintos; los límites
de precio admiten 0-1000000000 con máximo dos decimales. Los productos se distribuyen
equilibradamente entre las categorías, con precios y existencias aleatorios.
Se devuelve 201 al terminar, con generationId, createdCount, categoryIds,
supplierId y elapsedMilliseconds. No se devuelve un arreglo de 100.000 registros.

Implementación:

- Generación mediante iterador: no se mantiene una lista de toda la carga ni
  se agregan 100.000 entidades al ChangeTracker.
- Inserción con EF mediante AddRange y SaveChangesAsync en lotes de 1.000 productos,
  liberando su seguimiento después de guardarlos, dentro de una sola transacción.
  Un fallo/cancelación antes del commit revierte la carga completa.
- Validación de referencias activas con CountAsync y AnyAsync, sin bloqueos explícitos.
  Se acepta que otra operación pueda desactivarlas después de la validación.
- Máximo dos generaciones simultáneas por instancia, sin cola; el exceso recibe 429.
- Consultas AsNoTracking, proyección a DTO y filtros/Skip/Take en PostgreSQL.
- Índice GIN con pg_trgm sobre SearchName calculado para búsqueda parcial, índices
  por categoría/estado y precio/estado, y claves de desempate.

La operación es síncrona a nivel HTTP y asíncrona en acceso a datos. No es un job
en segundo plano ni es idempotente: repetir la petición genera otra carga. Si se
pierde la respuesta después del commit, comprobar los registros antes de reintentar.
Las migraciones habilitan la extensión pg_trgm; el usuario de migraciones necesita
permisos para instalarla.

Verificación local en Docker (23-09-2026): 100.000 productos creados en 4.798 ms
medidos por el servicio (4.815 ms por HTTP), distribuidos 50.000/50.000 en las dos
categorías. Una página filtrada y ordenada de 25 productos tardó 229 ms por HTTP.
Se comprobó la última página y el total, junto con CRUD, imagen, validaciones,
referencias inactivas y eliminación lógica: 32 comprobaciones HTTP satisfactorias.
Es una medición puntual local, no una garantía de latencia ni una prueba de
concurrencia. Los datos generados permanecen disponibles en el volumen local.

## Escalamiento horizontal en cloud

Desplegar réplicas de la API detrás de un balanceador con TLS. Las réplicas
comparten PostgreSQL administrado y la misma configuración JWT desde un gestor
de secretos; no necesitan afinidad de sesión. La verificación de empleado/rol
activo consulta la base compartida, por lo que el bloqueo aplica a todas.

Aplicar migraciones una sola vez por despliegue y desactivar
Database__ApplyMigrations en las réplicas. Limitar sus pools de conexiones según
la capacidad de PostgreSQL. Ajustar réplicas con métricas de latencia, CPU,
conexiones y presión de escritura; agregar réplicas de API no aumenta por sí solo
la capacidad de escritura de la base.

El límite actual de generación es por instancia. Para cargas mayores o frecuentes,
usar una cola duradera y workers con concurrencia global acotada, persistir el
estado del job e incorporar claves de idempotencia. El endpoint devolvería 202
con un identificador consultable. Centralizar también el límite de login en el
gateway o Redis. No se incorpora caché todavía; de agregarse, deberá compartirse
y contemplar invalidación de productos/categorías.

## Fechas de auditoría

Todas las entidades heredan de AuditTable a través de Entity. Es una clase base,
no una tabla independiente ni un historial de cambios. Cada una de las nueve
tablas incorpora:

- CreationDate: DateTime UTC, generado al insertar e inmutable.
- UpdatedDate: DateTime? UTC, null al crear y actualizado cuando cambia algún
  dato, incluido Active para eliminaciones lógicas y reactivaciones.

PostgreSQL administra estas fechas con triggers; cubren EF Core y escrituras
desde DBeaver. EF recupera los valores generados y no permite sobrescribirlos.
Un UPDATE sin cambios reales no cambia UpdatedDate; modificar exclusivamente
las fechas tampoco permite falsificarlas. Los DTOs de entrada no admiten fechas.

Los registros anteriores a esta migración reciben la fecha de aplicación como
CreationDate y UpdatedDate=null: no hay datos históricos para recuperar sus fechas
originales. Se preservan las filas y sus relaciones.

## Proveedores y clientes

GET /Suppliers y GET /Customers devuelven listas paginadas. GET /Suppliers/{id}
y GET /Customers/{id} devuelven el detalle. Los GET requieren JWT. Para Customers,
POST, PUT, DELETE y la inserción masiva requieren Admin (nombre exacto almacenado).
Todas las operaciones de Suppliers requieren JWT, sin restricción de rol.
POST /UserAdministration/Register y POST /Login siguen siendo públicos.
POST /UserAdministration/Users permite a Admin crear usuarios con un rol asignado;
el registro público siempre asigna User.

| Método | Proveedores | Clientes |
|---|---|---|
| GET / POST | /Suppliers | /Customers |
| GET / PUT / DELETE | /Suppliers/{id} | /Customers/{id} |
| POST | /Suppliers/Bulk | /Customers/Bulk |

Filtros de ambos listados: page, pageSize (máximo 100), search, city y country.
Search busca parcialmente por empresa/contacto y también por código de cliente.
City y country comparan sin distinguir mayúsculas; el orden estable es por id.
Se excluyen registros inactivos. Las respuestas incluyen todos los campos de
contacto, CreationDate y UpdatedDate.

Crear proveedor:

```json
{
  "companyName": "Proveedor de infraestructura",
  "contactName": "Ana Pérez",
  "city": "Bogota",
  "country": "Colombia",
  "phone": "3001234567",
  "homePage": "https://example.com"
}
```

Crear cliente:

```json
{
  "companyName": "Cliente empresarial",
  "contactName": "Carlos López",
  "city": "Medellin",
  "country": "Colombia"
}
```

CompanyName es obligatorio. También se admiten contactTitle, address, region,
postalCode y fax. Los proveedores admiten homePage y su id es autogenerado.
CustomerId es un entero autoincremental generado por PostgreSQL. No se envía al
crear clientes; la respuesta devuelve el campo id numérico, usado en las rutas
/Customers/{id}. Esto también aplica a la inserción masiva.
La migración CustomerIntegerIdentity asigna nuevos IDs a los clientes existentes,
incluidos los inactivos, y actualiza Orders.CustomerId conservando las relaciones
y las fechas de auditoría. Los códigos anteriores se reemplazan; para revertir
esta migración se requiere restaurar un respaldo previo.
PUT reemplaza los campos de contacto; los opcionales omitidos quedan en null.

POST /Suppliers/Bulk recibe directamente un arreglo de objetos de proveedor.
POST /Customers/Bulk recibe directamente un arreglo de objetos de cliente:

```json
[
  { "companyName": "Cliente dos" },
  { "companyName": "Cliente tres" }
]
```

Cada lote admite 1-1000 objetos, valida todos antes de guardar y devuelve 201 con
createdCount e items (incluidos los ids generados). Se utiliza AddRange y un único
SaveChanges transaccional: los errores rechazan el lote completo.
Un lote inválido devuelve 400; los conflictos de relaciones devuelven 409.
DELETE es lógico y actualiza automáticamente UpdatedDate.

## Transportadoras (Shippers)

Todos los endpoints requieren JWT y permiten los roles autenticados, como Suppliers.

| Método | Ruta | Operación |
|---|---|---|
| GET | /Shippers?search=transporte&page=1&pageSize=20 | Listado de activos, búsqueda por nombre o teléfono y paginación |
| GET | /Shippers/{id} | Detalle de una transportadora activa |
| POST | /Shippers | Crear; devuelve 200 con el ID generado y las fechas de auditoría |
| PUT | /Shippers/{id} | Reemplazar nombre y teléfono; devuelve 200 |
| DELETE | /Shippers/{id} | Inactivar; devuelve 204 |

POST y PUT reciben:

```json
{
  "companyName": "Transportadora ejemplo",
  "phone": "3001234567"
}
```

CompanyName es obligatorio y admite hasta 200 caracteres; Phone es opcional y
admite hasta 30. PUT deja Phone en null si se omite. El listado devuelve items,
totalCount, page y pageSize; permite páginas de 1 a 100 registros.

La inactivación utiliza EntityStatus.SetActive y devuelve 409 si existe una orden
activa cuyo ShipVia referencia la transportadora. Las órdenes inactivas no la
bloquean y conservan su relación histórica. Las consultas por ID de registros
inactivos o inexistentes devuelven 404. No se requiere una migración adicional.

## Órdenes: cabecera y detalles

Todos los endpoints requieren JWT y permiten cualquier rol autenticado.
Solo se consultan y modifican órdenes y detalles activos; no hay reactivación.

| Método | Ruta | Operación |
|---|---|---|
| POST | /Orders | Crear cabecera con detalles; devuelve 200 |
| GET | /Orders | Listar cabeceras paginadas con sus detalles activos |
| GET | /Orders/{id} | Consultar cabecera y detalles activos |
| PUT | /Orders/{id} | Actualizar cabecera, modificar detalles y agregar nuevos; devuelve 200 |
| DELETE | /Orders/{id} | Inactivar orden y detalles activos; devuelve 204 |
| DELETE | /Orders/{orderId}/Details/{detailId} | Inactivar solo un detalle; devuelve 204 |

Ejemplo de creación (sustituir los IDs por registros activos):

```json
{
  "customerId": 1,
  "shipVia": 1,
  "orderDate": "2026-09-24T12:00:00Z",
  "freight": 5,
  "shipName": "Oficina principal",
  "shipCity": "Bogota",
  "details": [
    { "productId": 1, "quantity": 2, "discount": 0.1 }
  ]
}
```

EmployeeId se asigna desde el JWT al crear y se conserva al actualizar. CustomerId,
OrderDate y un arreglo de 1-1000 detalles son obligatorios. ShipVia es opcional.
Las fechas se envían en UTC; RequiredDate y ShippedDate no pueden preceder OrderDate.
Freight admite hasta dos decimales, no negativos. Quantity debe ser positiva;
Discount es una fracción entre 0 y 1, con hasta cuatro decimales.

PUT reemplaza los campos de la cabecera. Cada elemento de details con OrderDetailId
modifica ese detalle activo de la orden; sin OrderDetailId agrega uno nuevo.
Los detalles omitidos permanecen intactos. ProductId no se cambia en un detalle
existente: se inactiva el detalle y se agrega uno nuevo. Los IDs de detalles
inexistentes, inactivos o pertenecientes a otra orden devuelven 404.

El índice único parcial impide repetir productos activos en una orden (409).
Un producto previamente inactivado puede agregarse como un detalle con un ID nuevo.
Inactivar el último detalle deja la cabecera activa, con un arreglo vacío al consultar.
Inactivar la orden inactiva todos sus detalles en una transacción. Las escrituras
de cabecera y detalles se guardan de forma atómica, sin eliminación física.

El precio se toma del producto al agregarlo y se conserva en las actualizaciones.
No se modifica inventario. Cada total de línea se calcula como cantidad × precio ×
(1 − descuento), redondeado a dos decimales alejándose de cero en los puntos medios;
el total de la orden suma esas líneas y Freight. Las respuestas incluyen IDs de
referencias y fechas de auditoría, sin exponer entidades relacionadas inactivas.

GET /Orders admite page, pageSize (1-100), search (ID, ShipName o ShipCity),
customerId, employeeId, shipVia, fromDate y toDate (UTC, límites inclusivos).
La paginación corresponde a las cabeceras y cada una incluye sus detalles activos.
Las referencias se validan mediante consultas normales, sin bloqueos explícitos.

La migración OrderDetailIdentity asigna IDs a los detalles existentes, conserva
sus datos y relaciones, y sustituye la PK compuesta por el índice único parcial.
Revertirla solo es posible si no existen pares OrderId/ProductId repetidos,
incluyendo inactivos; de lo contrario la reversión falla sin eliminar esos datos.

## Mi perfil y matriz de acceso

| Funcionalidad | Acceso |
|---|---|
| Login y registro | Público |
| GET de usuarios y Customers | Cualquier usuario autenticado |
| Crear/editar/eliminar Customers, incluida carga masiva | Admin |
| Crear administrativamente/modificar/desactivar/reactivar otros usuarios | Admin |
| Restablecer contraseña mediante Users/ResetPassword | Público |
| Gestión de roles, incluidos GET | Admin |
| Suppliers, productos y categorías: todas sus operaciones | Cualquier usuario autenticado |
| GET /Profile, PUT /Profile, PUT /Profile/Password | Cualquier usuario autenticado, solo su cuenta |

Profile obtiene la identidad exclusivamente del claim sub del JWT, sin aceptar
un id de usuario como destino. Los DTO de perfil rechazan propiedades desconocidas,
incluidos id, roleId, active y passwordHash. GET devuelve datos personales, rol de
solo lectura y fechas de auditoría; nunca contraseñas ni hashes.

PUT /Profile reemplaza los campos personales permitidos; los opcionales omitidos
quedan en null. Email debe ser único, también respecto a empleados inactivos.

```json
{
  "firstName": "Ana",
  "lastName": "Pérez",
  "email": "ana@example.com",
  "birthDate": "1995-04-15",
  "address": "Calle 10",
  "city": "Bogota",
  "region": "Cundinamarca",
  "postalCode": "110111",
  "country": "Colombia",
  "homePhone": "3001234567"
}
```

PUT /Profile/Password exige JWT y la contraseña actual, sin restricción de rol:

```json
{
  "currentPassword": "ClaveActual2026!",
  "newPassword": "NuevaClavePersonal2026!"
}
```

Devuelve 204 si cambia, 400 si la contraseña actual es incorrecta o la nueva no
cumple la longitud de 12-128 caracteres. El endpoint separado Users/ResetPassword
permite restablecer por correo o id sin JWT, según el alcance acordado para la prueba.
Como no hay SecurityStamp, el cambio no revoca JWT ya emitidos.

## Integración continua

El workflow `.github/workflows/ci.yml` se ejecuta con pushes a `main`, pull requests
hacia `main` y manualmente desde GitHub Actions. El job `build` instala .NET 10,
restaura dependencias, compila la solución en Release, verifica el formato,
ejecuta las pruebas unitarias y de integración y construye la imagen
`finanzauto-api:ci`. Cualquier fallo bloquea los pasos posteriores. Los resultados
TRX se publican como el artefacto `test-results` durante 14 días, incluso si falla
una prueba, mediante [upload-artifact](https://github.com/actions/upload-artifact).

`global.json` limita el SDK a versiones estables de .NET 10. Las acciones se
configuran siguiendo la [documentación de setup-dotnet](https://github.com/actions/setup-dotnet).

Para reproducir las verificaciones desde la raíz:

```powershell
dotnet restore Finanzauto.slnx
dotnet build Finanzauto.slnx --configuration Release --no-restore
dotnet format Finanzauto.slnx --verify-no-changes --no-restore
dotnet test Finanzauto.slnx --configuration Release --no-build --logger trx --results-directory artifacts/TestResults
docker build --file Finanzauto/Dockerfile --tag finanzauto-api:ci .
```

Si falla el formato, ejecutar `dotnet format Finanzauto.slnx --no-restore` y revisar
los cambios antes de subirlos. El job `build` no requiere secrets.

Después de `build`, el job `smoke` se ejecuta solamente en pushes a `main` y
ejecuciones manuales sobre `main`. Los pull requests ejecutan únicamente `build`.
El job referencia el Environment `FinanzautoEnv`. Configurar en
**Settings > Environments > FinanzautoEnv**, en sus secciones de secrets y variables:

| Tipo | Nombre | Requisito |
| --- | --- | --- |
| Secret | `CI_POSTGRES_PASSWORD` | Contraseña de la base temporal |
| Secret | `CI_JWT_SIGNING_KEY` | Clave aleatoria de al menos 32 bytes |
| Secret | `CI_BOOTSTRAP_ADMIN_PASSWORD` | Contraseña de 12 a 128 caracteres |
| Variable | `CI_BOOTSTRAP_ADMIN_EMAIL` | Correo válido del administrador temporal |

Los valores se pasan por variables de entorno siguiendo la
[documentación de GitHub Secrets](https://docs.github.com/en/actions/how-tos/write-workflows/choose-what-workflows-do/use-secrets).
Si falta alguno, la verificación falla e indica su nombre sin revelar valores.

`compose.ci.yml` y `.github/scripts/smoke.py` construyen la API y levantan PostgreSQL
17 en el proyecto aislado `finanzauto-ci`, aplican migraciones sobre una base vacía
y crean el administrador. Comprueban `/health`, rechazo de `/Profile` sin JWT
(401), login y consulta del perfil con JWT (200 y rol Admin). El puerto HTTP es
dinámico y PostgreSQL no publica puerto. No se lee el `.env` local ni se inicia el
frontend. Al finalizar, incluso si una comprobación falla, se eliminan los
contenedores, red y volumen temporales; el workflow repite la limpieza con
`always()`. No se imprimen tokens, respuestas de login ni logs de contenedores.

Para reproducir este paso, definir las cuatro variables de entorno anteriores y
ejecutar `python .github/scripts/smoke.py` desde la raíz con Docker disponible.
Reservar el nombre de proyecto `finanzauto-ci` para esta verificación desechable.
Este pipeline no publica imágenes ni realiza despliegues. Las pruebas xUnit se
ejecutan antes de este job, también en pull requests, sin GitHub Secrets.

## Pruebas automatizadas

La guía [Recorrido de los mecanismos del backend](docs/recorrido-backend.md) explica la carga masiva, los bloqueos, la eliminación lógica, la auditoría y la autenticación para seguir el código.

Requisitos: SDK .NET 10 y Docker con contenedores Linux para integración. No es
necesario levantar el Compose de desarrollo ni definir credenciales locales.

```powershell
# Todas las pruebas, incluida la carga de 100.000 productos
dotnet test Finanzauto.slnx --configuration Release

# Solo unitarias (no requieren Docker)
dotnet test Finanzauto.UnitTests/Finanzauto.UnitTests.csproj --configuration Release

# Solo integración (requiere Docker)
dotnet test Finanzauto.IntegrationTests/Finanzauto.IntegrationTests.csproj --configuration Release

# Omitir únicamente la carga masiva durante una iteración local
dotnet test Finanzauto.slnx --configuration Release --filter "Category!=Bulk"
```

`Finanzauto.UnitTests` cubre login y rechazo de credenciales, usuario/rol inactivo,
registro, duplicados, protección de roles base y de la cuenta propia, perfil y
cambio de contraseña, límites y validación de lotes, generación aleatoria y
validación decimal en es-CO, es-ES y en-US. Moq sustituye las dependencias externas
y comprueba que los rechazos no escriben datos ni emiten tokens.

`Finanzauto.IntegrationTests` utiliza JWT real, `WebApplicationFactory` y
[Testcontainers PostgreSQL](https://dotnet.testcontainers.org/examples/aspnet/).
Un contenedor PostgreSQL 17 sirve a la colección; cada prueba tiene una base nueva,
sus migraciones y su administrador con contraseña temporal. Se eliminan las bases
y el contenedor al finalizar. Se verifican permisos Admin/User/rol personalizado,
perfil, revocación por desactivación, CRUD, filtros/paginación, foto de categoría,
eliminación lógica, auditoría EF/SQL, cascada física y rollback entre lotes EF ante
una fila inválida. La carga de 100.000 productos comprueba cantidad, distribución
entre SERVIDORES/CLOUD, precios y auditoría; no impone un umbral de rendimiento
dependiente del equipo. Docker no disponible hace fallar las pruebas de integración;
no se omiten silenciosamente.

`ContractCompatibilityTests` añade 16 comprobaciones de serialización contra
`ResponseContracts.json`, capturado a partir de los DTOs anteriores al refactor.
Comprueba los nombres y tipos JSON de las respuestas, incluidos los objetos
anidados, las fechas, la paginación y las imágenes en base64. Estas pruebas
complementan las de integración que comprueban los datos devueltos por la API.

`QueryTests` comprueba las seis combinaciones de ordenamiento de productos,
desempates entre páginas, filtros combinados, páginas vacías, referencias inactivas
y coincidencia entre listado y detalle de clientes/proveedores. También captura
los comandos ejecutados por EF para verificar que PostgreSQL recibe los filtros
y `LIMIT/OFFSET`, con un conteo y una consulta de página, sin cargar imágenes en
el listado de productos.

## Compose unificado: front, API y PostgreSQL

El Compose de esta carpeta agrupa los servicios `front`, `api` y `db` bajo el proyecto `finanzauto`. Cada servicio conserva su imagen y contenedor. La red interna es `finanzauto_default` y el volumen existente sigue siendo `finanzauto_postgres_data`.

Configurar `FRONT_CONTEXT` en `.env` con la carpeta del frontend que contiene su `Dockerfile`. Puede ser una ruta absoluta entre comillas o una relativa a este Compose; el ejemplo usa `../Front`. La configuraciÃ³n local apunta a la ubicaciÃ³n actual del frontend, sin copiar ni mover su cÃ³digo.

Desde esta carpeta:

```powershell
docker compose up --build -d
docker compose ps
```

- Portal React/Nginx: http://localhost:5174
- API/Swagger: http://localhost:8080/swagger
- PostgreSQL: localhost:5432

Nginx reenvÃ­a `/api/` al servicio `api:8080`; el navegador trabaja con el mismo origen. El frontend depende del arranque de la API y la API espera a PostgreSQL saludable. Para actualizar solo el frontend: `docker compose up --build -d --no-deps front`.

`docker compose down` detiene el conjunto conservando el volumen. No usar `down -v` si se quieren conservar los datos.
