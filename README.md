# Sistema de gestión de colaboradores y tareas

Aplicación fullstack con **.NET 8 Web API**, **Angular** y **SQL Server**. La solución permite registrar colaboradores, crear tareas asignadas, consultar y filtrar tareas, y controlar su avance mediante los estados `Pending`, `InProgress` y `Done`.

El proyecto prioriza una implementación clara, mantenible y verificable como un MVP: API REST, separación por capas, persistencia reproducible, manejo de errores consistente, frontend funcional y uso de funcionalidades JSON nativas de SQL Server.

## Tabla de contenido

- [Stack técnico](#stack-técnico)
- [Requisitos previos](#requisitos-previos)
- [Estructura del proyecto](#estructura-del-proyecto)
- [Configuración y ejecución](#configuración-y-ejecución)
- [Verificación](#verificación)
- [Funcionalidades implementadas](#funcionalidades-implementadas)
- [API REST](#api-rest)
- [Base de datos y JSON en SQL Server](#base-de-datos-y-json-en-sql-server)
- [Decisiones técnicas](#decisiones-técnicas)
- [Alcance y pendientes](#alcance-y-pendientes)

## Stack técnico

| Capa | Tecnología |
|---|---|
| Backend | .NET 8 Web API |
| Frontend | Angular |
| Base de datos | SQL Server Express |
| API | REST |
| Pruebas backend | xUnit / ASP.NET Core testing |
| Pruebas frontend | Angular test runner |
| Validación UI runtime | Playwright script |

## Requisitos previos

- .NET 8 SDK.
- Node.js `24.19.0` y npm `11.17.0`.
- SQL Server Express con autenticación de Windows.
- SQL Server Tools con el ejecutable ODBC `SQLCMD.EXE`.

> Las versiones de Node.js y npm indicadas son las verificadas durante el desarrollo. Otras versiones pueden funcionar, pero no fueron validadas para esta entrega.

## Estructura del proyecto

```text
/
├── backend/                  # API .NET, dominio, infraestructura y pruebas
├── frontend/task-management/ # Aplicación Angular
├── database/create.sql       # Script reproducible de SQL Server
├── docs/constitution.md      # Principios de ingeniería del proyecto
├── specs/001-task-management # Especificación, plan y tareas SDD
└── README.md
```

## Configuración y ejecución

### 1. Crear la base de datos

Desde la raíz del repositorio, ejecutar en PowerShell:

```powershell
$sqlcmd = Get-Command sqlcmd.exe -All |
  Where-Object { $_.Source -like '*\Microsoft SQL Server\Client SDK\ODBC\*\Tools\Binn\SQLCMD.EXE' } |
  Select-Object -First 1 -ExpandProperty Source

if (-not $sqlcmd) { throw 'No se encontró SQLCMD.EXE de SQL Server ODBC Tools.' }

& $sqlcmd -S ".\SQLEXPRESS" -E -C -b -d master -Q "IF DB_ID(N'TaskManagement') IS NULL CREATE DATABASE TaskManagement;"
& $sqlcmd -S ".\SQLEXPRESS" -E -C -b -d TaskManagement -i database\create.sql
```

El script `database/create.sql` es idempotente. Crea las tablas `Users` y `Tasks`, define claves primarias, clave foránea, restricciones, validación JSON e índices requeridos.

> En el entorno verificado, el `sqlcmd` moderno basado en Go (`v1.10.0`) agotó el tiempo de espera al resolver la instancia nombrada `.\SQLEXPRESS`. El ejecutable ODBC `SQLCMD.EXE` localizado por el comando anterior conectó correctamente. Por eso se recomienda no usar el primer `sqlcmd` disponible en `PATH` sin confirmar la variante.

### 2. Ejecutar la API

```powershell
$env:ConnectionStrings__TaskManagement="Server=.\SQLEXPRESS;Database=TaskManagement;Integrated Security=True;TrustServerCertificate=True"
dotnet run --project backend\src\TaskManagement.Api --urls http://localhost:5100
```

### 3. Ejecutar el frontend

En otra terminal:

```powershell
cd frontend\task-management
npm ci
npx playwright install chromium
npm start -- --port 4200 --proxy-config proxy.conf.json
```

Abrir la aplicación en:

```text
http://localhost:4200
```

## Verificación

### Backend

```powershell
cd backend
dotnet restore
dotnet build
dotnet test
```

### Frontend

```powershell
cd frontend\task-management
npm ci
npm test -- --watch=false
npm run build
```

### Flujo UI con API real

Con la API en `http://localhost:5100` y Angular en `http://localhost:4200`:

```powershell
cd frontend\task-management
node e2e\runtime-flow.mjs
```

## Funcionalidades implementadas

### Usuarios

- Crear usuarios con nombre y correo electrónico.
- Listar usuarios.
- Validar duplicados de correo normalizado.

### Tareas

- Crear tareas con título obligatorio.
- Asignar cada tarea a un usuario existente.
- Listar tareas.
- Filtrar tareas por usuario, estado y prioridad.
- Ordenar tareas por fecha de creación.
- Cambiar estado de tarea respetando el flujo permitido.
- Rechazar la transición directa `Pending -> Done` desde el backend.

### Frontend Angular

- Listado de tareas.
- Filtro por estado y prioridad.
- Formulario reactivo para crear tareas.
- Selección de usuario desde el listado de colaboradores.
- Cambio de estado de tareas.
- Visualización básica de errores de API.

## API REST

| Método | Endpoint | Descripción |
|---|---|---|
| `POST` | `/api/users` | Crea un usuario. |
| `GET` | `/api/users` | Lista usuarios. |
| `POST` | `/api/tasks` | Crea una tarea asignada a un usuario. |
| `GET` | `/api/tasks` | Lista tareas y permite filtros por usuario, estado y prioridad. |
| `PUT` | `/api/tasks/{id}/status` | Cambia el estado de una tarea. |
| `PATCH` | `/api/tasks/{id}/additional-info` | Actualiza una propiedad del JSON adicional de una tarea. |

Los controladores usan DTOs en el límite HTTP y devuelven errores consistentes mediante `ProblemDetails`.

## Base de datos y JSON en SQL Server

La base de datos contiene las tablas `Users` y `Tasks`. La relación entre tareas y usuarios se implementa mediante clave foránea. El índice `(UserId, Status, CreatedAt DESC, Id DESC)` soporta la consulta requerida por usuario, estado y fecha de creación.

La tabla `Tasks` incluye la columna:

```sql
AdditionalInfo NVARCHAR(MAX) NULL
```

Esta columna almacena metadatos flexibles de la tarea, como prioridad, fecha estimada de finalización, etiquetas y metadatos libres. Los campos esenciales (`Title`, `Status`, `UserId` y `CreatedAt`) permanecen como columnas relacionales.

### Ejemplos SQL de JSON

El script `database/create.sql` valida y consulta JSON con funciones nativas de SQL Server:

```sql
-- Validar que el documento almacenado sea JSON válido.
SELECT Id, ISJSON(AdditionalInfo) AS IsValidJson
FROM dbo.Tasks;

-- Leer y filtrar una propiedad escalar del JSON.
SELECT Id, JSON_VALUE(AdditionalInfo, '$.priority') AS Priority
FROM dbo.Tasks
WHERE JSON_VALUE(AdditionalInfo, '$.priority') = N'High';

-- Leer un arreglo JSON sin convertirlo a texto relacional.
SELECT Id, JSON_QUERY(AdditionalInfo, '$.tags') AS Tags
FROM dbo.Tasks
WHERE JSON_QUERY(AdditionalInfo, '$.tags') IS NOT NULL;

-- Expandir cada etiqueta como una fila.
SELECT t.Id, tag.[value] AS Tag
FROM dbo.Tasks AS t
CROSS APPLY OPENJSON(t.AdditionalInfo, '$.tags') AS tag;

-- Actualizar solo una propiedad y conservar las demás.
DECLARE @Metadata NVARCHAR(MAX) = N'{"priority":"Medium","tags":["backend"]}';
SELECT JSON_MODIFY(@Metadata, '$.priority', N'High') AS UpdatedJson;
```

Funciones demostradas:

- `ISJSON` para validar contenido JSON.
- `JSON_VALUE` para leer y filtrar propiedades escalares.
- `JSON_QUERY` para consultar arreglos u objetos JSON.
- `OPENJSON` para expandir arreglos JSON.
- `JSON_MODIFY` para demostrar la actualización de una propiedad específica.

## Decisiones técnicas

- **Separación por capas:** la API mantiene controladores delgados; la lógica de negocio y validaciones principales viven en Core; Infrastructure encapsula EF Core y SQL Server.
- **Reglas de negocio centralizadas:** las transiciones de estado se validan en backend, no únicamente en la interfaz Angular.
- **DTOs en la frontera HTTP:** la API evita exponer entidades de persistencia como contrato público accidental.
- **Errores consistentes:** `ProblemDetails` unifica las respuestas de error sin exponer stack traces, cadenas de conexión ni detalles internos.
- **Persistencia reproducible:** `database/create.sql` permite recrear el esquema, restricciones, índices y ejemplos SQL requeridos.
- **JSON relacionalmente acotado:** `AdditionalInfo` se usa solo para información flexible; no reemplaza campos esenciales del modelo.
- **Frontend simple y funcional:** Angular consume la API mediante servicios y usa formularios reactivos para la creación de tareas.

## Matriz de evidencia

| Requisito | Evidencia principal | Resultado |
|---|---|---|
| Gestión de usuarios | `UserFlowTests.cs`; alta y listado desde UI real | PASS |
| Gestión de tareas | `TaskFlowTests.cs`; creación asignada y listado | PASS |
| Estados y transición prohibida | `StatusFlowTests.cs`; validación backend de `Pending -> Done` | PASS |
| API REST | Pruebas de controladores y E2E API | PASS |
| SQL Server | `database/create.sql`; pruebas de esquema y consultas | PASS |
| JSON en SQL Server | `ISJSON`, `JSON_VALUE`, `JSON_QUERY`, `OPENJSON`, `JSON_MODIFY` | PASS |
| Frontend Angular | Tests frontend y flujo runtime con API real | PASS |
| Manejo de errores | Pruebas API para `400`, `404`, `409` y errores visibles en UI | PASS |

