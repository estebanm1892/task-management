# Gestión de colaboradores y tareas

Aplicación full-stack con API .NET 8, Angular y SQL Server. Permite registrar colaboradores, crear y consultar tareas, filtrar por estado/prioridad y aplicar el ciclo `Pending → InProgress → Done`.

## Requisitos

- .NET 8 SDK
- Node.js 24.19.0 y npm 11.17.0 (versiones exactas verificadas; otras versiones no fueron validadas)
- SQL Server Express con autenticación de Windows
- SQL Server Tools con el ejecutable ODBC `SQLCMD.EXE`

## Puesta en marcha

Desde la raíz, crear la base y aplicar el script idempotente:

```powershell
$sqlcmd = Get-Command sqlcmd.exe -All |
  Where-Object { $_.Source -like '*\Microsoft SQL Server\Client SDK\ODBC\*\Tools\Binn\SQLCMD.EXE' } |
  Select-Object -First 1 -ExpandProperty Source
if (-not $sqlcmd) { throw 'No se encontró SQLCMD.EXE de SQL Server ODBC Tools.' }

& $sqlcmd -S ".\SQLEXPRESS" -E -C -b -d master -Q "IF DB_ID(N'TaskManagement') IS NULL CREATE DATABASE TaskManagement;"
& $sqlcmd -S ".\SQLEXPRESS" -E -C -b -d TaskManagement -i database\create.sql
```

El script reproducible está en [database/create.sql](database/create.sql). Crea `Users` y `Tasks`, define claves primarias y foránea, añade los índices requeridos y es idempotente.

En el entorno verificado, el `sqlcmd` moderno basado en Go (v1.10.0) agotó el tiempo de espera al resolver la instancia nombrada `.\SQLEXPRESS`; el `SQLCMD.EXE` ODBC localizado por el comando anterior conectó correctamente. No use el primer `sqlcmd` disponible en `PATH` sin comprobar qué variante es.

Iniciar la API:

```powershell
$env:ConnectionStrings__TaskManagement="Server=.\SQLEXPRESS;Database=TaskManagement;Integrated Security=True;TrustServerCertificate=True"
dotnet run --project backend\src\TaskManagement.Api --urls http://localhost:5100
```

En otra terminal, iniciar Angular con el proxy versionado:

```powershell
cd frontend\task-management
npm ci
npx playwright install chromium
npm start -- --port 4200 --proxy-config proxy.conf.json
```

Abrir `http://localhost:4200`.

## Verificación

```powershell
cd backend
dotnet restore
dotnet build
dotnet test

cd ..\frontend\task-management
npm ci
npm test -- --watch=false
npm run build
```

El flujo UI repetible requiere la API y Angular activos en los puertos anteriores:

```powershell
node e2e\runtime-flow.mjs
```

## Diseño y JSON

- La API mantiene controladores delgados; Core concentra validación y transiciones; Infrastructure aísla EF Core/SQL Server.
- `ProblemDetails` unifica errores sin exponer excepciones, SQL ni conexiones.
- `Tasks.AdditionalInfo` conserva metadatos flexibles; título, estado, usuario y fecha siguen siendo relacionales.
- `PATCH /api/tasks/{id}/additional-info` actualiza una propiedad JSON, conserva las demás y rechaza propiedades relacionales esenciales.
- `database/create.sql` valida JSON con `ISJSON`, lee/filtra prioridad con `JSON_VALUE`, devuelve etiquetas con `JSON_QUERY` y las expande con `OPENJSON`.
- `database/create.sql` incluye una demostración de `JSON_MODIFY` que actualiza `priority` y conserva `tags`.
- El índice `(UserId, Status, CreatedAt DESC, Id DESC)` soporta filtros combinados y orden determinista.

### Ejemplos SQL de JSON en SQL Server

`Tasks.AdditionalInfo` es `NVARCHAR(MAX)`. El script aplica estas restricciones y consultas nativas:

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

La columna `AdditionalInfo` conserva prioridad, fechas estimadas, etiquetas y metadatos libres; `Title`, `Status`, `UserId` y `CreatedAt` permanecen como columnas relacionales.

## Alcance y pendientes

El alcance obligatorio está implementado y verificado. Autenticación, roles, eliminación, reasignación y transiciones adicionales están fuera de alcance. `npm ci` reportó una vulnerabilidad alta en el árbol de dependencias; queda pendiente revisar una actualización compatible sin ampliar esta entrega.

## Matriz RF/evidencias

| RF | Evidencia principal | Resultado |
|---|---|---|
| RF-1 | `UserFlowTests.cs`; alta y duplicado por mayúsculas en UI real | PASS |
| RF-2 | `UserFlowTests.cs`; selección de colaborador cargada por Angular | PASS |
| RF-3 | `TaskFlowTests.cs`; creación asignada mediante UI real | PASS |
| RF-4 | `TaskFlowTests.cs`; listado y filtros API/UI | PASS |
| RF-5 | consulta SQL y E2E ordenados por fecha descendente | PASS |
| RF-6 | `StatusFlowTests.cs`; avance UI y salto prohibido | PASS |
| RF-7 | filtro UI y demostraciones `ISJSON`/`JSON_VALUE`/`JSON_QUERY`/`OPENJSON` | PASS |
| RF-8 | pruebas API para 400/404; E2E real para 409 y error visible en Angular | PASS |
| RF-9 | API+SQL reales, consultas posteriores, builds y contrato `ProblemDetails` | PASS |

### Actualización opcional de JSON

Request:

```json
{
  "property": "priority",
  "value": "High"
}
```

Endpoint: `PATCH /api/tasks/{id}/additional-info`.

La operación conserva el resto de `AdditionalInfo`, valida que `priority` sea `Low`, `Medium` o `High`, y rechaza `Title`, `Status`, `UserId` y `CreatedAt`. Las pruebas `TaskAdditionalInfoApiTests` cubren actualización, conservación, tarea inexistente, propiedades esenciales y prioridad inválida.
