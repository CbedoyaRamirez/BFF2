# Guía de Sistema de Logging

## Descripción General

El sistema de logging está configurado con **Serilog** y organiza los logs en carpetas específicas según su propósito:

- `logs/` - Logs generales de la aplicación
- `logs/error/` - Logs de errores (Error y Fatal)
- `logs/performance/` - Logs de métricas de rendimiento
- `logs/dev/` - Logs para desarrollo y debugging

## Estructura de Carpetas

```
Chubb.Bot.AI.Assistant.Api/
├── logs/
│   ├── app-YYYYMMDD.log          # Logs generales (todos los niveles)
│   ├── error/
│   │   └── error-YYYYMMDD.log    # Solo errores (Error y Fatal)
│   ├── performance/
│   │   └── performance-YYYYMMDD.log  # Métricas de rendimiento
│   └── dev/
│       └── dev-YYYYMMDD.log      # Logs de desarrollo
```

## Configuración

### Retención de Archivos

- **Logs generales**: 30 días
- **Logs de error**: 90 días
- **Logs de performance**: 30 días
- **Logs de desarrollo**: 7 días

### Rotación

Todos los logs rotan diariamente (cada día crea un nuevo archivo con la fecha en el nombre).

## Uso de LoggingHelper

La clase `LoggingHelper` proporciona métodos convenientes para cada tipo de log.

### 1. Logs de Error

Los logs de error se escriben automáticamente en `logs/error/`:

```csharp
using Chubb.Bot.AI.Assistant.Api.Helpers;

// Error simple
LoggingHelper.LogError("Error al procesar la solicitud del usuario {UserId}", userId);

// Error con excepción
try
{
    // código que puede fallar
}
catch (Exception ex)
{
    LoggingHelper.LogError("Error al conectar con el servicio externo", ex);
}

// Error fatal (para errores críticos)
LoggingHelper.LogFatal("Error crítico en el sistema", exception);
```

### 2. Logs de Performance

Los logs de performance miden automáticamente el tiempo de ejecución y se escriben en `logs/performance/`:

```csharp
using Chubb.Bot.AI.Assistant.Api.Helpers;

// Uso básico con using - mide automáticamente el tiempo
public async Task<IActionResult> GetData()
{
    using (LoggingHelper.LogPerformance("GetData Operation"))
    {
        // Tu código aquí
        var data = await _service.GetDataAsync();
        return Ok(data);
    }
    // Al salir del using, se loggea automáticamente el tiempo transcurrido
}

// Con contexto adicional
public async Task<IActionResult> ProcessRequest(int userId)
{
    using (var perfLogger = LoggingHelper.LogPerformance("ProcessRequest"))
    {
        perfLogger.AddContext("UserId", userId);
        perfLogger.AddContext("RequestType", "Standard");

        var result = await _service.ProcessAsync(userId);

        perfLogger.AddContext("RecordsProcessed", result.Count);

        return Ok(result);
    }
}
```

### 3. Logs de Desarrollo

Los logs de desarrollo son útiles para debugging y se escriben en `logs/dev/`:

```csharp
using Chubb.Bot.AI.Assistant.Api.Helpers;

// Log de desarrollo simple
LoggingHelper.LogDevelopment("Iniciando validación para usuario {UserId}", userId);

// Log de desarrollo con warning
LoggingHelper.LogDevelopmentWarning("Cache miss para la clave {CacheKey}", cacheKey);

// Log de un objeto completo (serializado como JSON)
var request = new ChatRequest { Message = "Hello", UserId = 123 };
LoggingHelper.LogDevelopmentObject("Request recibido", request);
```

### 4. Logs Estándar (ILogger)

También puedes usar el ILogger estándar de ASP.NET Core (estos van a los logs generales):

```csharp
public class MyController : ControllerBase
{
    private readonly ILogger<MyController> _logger;

    public MyController(ILogger<MyController> logger)
    {
        _logger = logger;
    }

    public IActionResult Get()
    {
        _logger.LogInformation("Obteniendo datos");
        _logger.LogWarning("Advertencia: Cache expirado");
        _logger.LogError("Error al procesar");
        return Ok();
    }
}
```

## Ejemplos Completos

### Ejemplo 1: Controller con Manejo de Errores y Performance

```csharp
using Chubb.Bot.AI.Assistant.Api.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace Chubb.Bot.AI.Assistant.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExampleController : ControllerBase
{
    private readonly ILogger<ExampleController> _logger;
    private readonly IExampleService _service;

    public ExampleController(ILogger<ExampleController> logger, IExampleService service)
    {
        _logger = logger;
        _service = service;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        using (var perfLogger = LoggingHelper.LogPerformance("GetById"))
        {
            try
            {
                perfLogger.AddContext("RequestId", id);

                LoggingHelper.LogDevelopment("Iniciando búsqueda para ID: {Id}", id);

                var result = await _service.GetByIdAsync(id);

                if (result == null)
                {
                    LoggingHelper.LogDevelopmentWarning("No se encontró registro con ID: {Id}", id);
                    return NotFound();
                }

                perfLogger.AddContext("RecordFound", true);

                return Ok(result);
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError("Error al obtener registro con ID: {Id}", ex, id);
                return StatusCode(500, "Error interno del servidor");
            }
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRequest request)
    {
        using (LoggingHelper.LogPerformance("Create Operation"))
        {
            try
            {
                LoggingHelper.LogDevelopmentObject("Request recibido", request);

                var result = await _service.CreateAsync(request);

                _logger.LogInformation("Registro creado exitosamente con ID: {Id}", result.Id);

                return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
            }
            catch (ValidationException vex)
            {
                LoggingHelper.LogDevelopmentWarning("Validación fallida: {Error}", vex.Message);
                return BadRequest(vex.Message);
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError("Error al crear registro", ex);
                return StatusCode(500, "Error interno del servidor");
            }
        }
    }
}
```

### Ejemplo 2: Middleware con Logging

```csharp
using Chubb.Bot.AI.Assistant.Api.Helpers;

public class CustomMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CustomMiddleware> _logger;

    public CustomMiddleware(RequestDelegate next, ILogger<CustomMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        using (var perfLogger = LoggingHelper.LogPerformance($"Request: {context.Request.Method} {context.Request.Path}"))
        {
            try
            {
                perfLogger.AddContext("Path", context.Request.Path.Value);
                perfLogger.AddContext("Method", context.Request.Method);

                await _next(context);

                perfLogger.AddContext("StatusCode", context.Response.StatusCode);
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError("Error en middleware para {Path}", ex, context.Request.Path);
                throw;
            }
        }
    }
}
```

### Ejemplo 3: Service con Performance y Error Logging

```csharp
using Chubb.Bot.AI.Assistant.Api.Helpers;

public class ExampleService : IExampleService
{
    private readonly ILogger<ExampleService> _logger;
    private readonly HttpClient _httpClient;

    public ExampleService(ILogger<ExampleService> logger, HttpClient httpClient)
    {
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task<Data> GetExternalDataAsync(string endpoint)
    {
        using (var perfLogger = LoggingHelper.LogPerformance("External API Call"))
        {
            perfLogger.AddContext("Endpoint", endpoint);

            try
            {
                LoggingHelper.LogDevelopment("Llamando a endpoint externo: {Endpoint}", endpoint);

                var response = await _httpClient.GetAsync(endpoint);

                perfLogger.AddContext("StatusCode", (int)response.StatusCode);
                perfLogger.AddContext("ResponseTime", response.Headers.Date);

                if (!response.IsSuccessStatusCode)
                {
                    LoggingHelper.LogError(
                        "Error en llamada externa. Endpoint: {Endpoint}, Status: {StatusCode}",
                        null,
                        endpoint,
                        response.StatusCode
                    );
                    throw new ExternalServiceException($"Error calling {endpoint}");
                }

                var data = await response.Content.ReadFromJsonAsync<Data>();

                perfLogger.AddContext("RecordsReceived", data?.Records?.Count ?? 0);

                return data;
            }
            catch (HttpRequestException hex)
            {
                LoggingHelper.LogError("Error de conexión con servicio externo: {Endpoint}", hex, endpoint);
                throw;
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError("Error inesperado al llamar servicio externo", ex);
                throw;
            }
        }
    }
}
```

## Formato de Logs

### Logs Generales y de Error
```
[2026-02-05 10:30:45.123 -05:00] [INF] [Namespace.ClassName] [correlation-id-123] Message with {Property} value {"Property": "value"}
```

### Logs de Performance
```
[2026-02-05 10:30:45.123 -05:00] Performance: GetData Operation completed in 245ms {"OperationName": "GetData Operation", "ElapsedMilliseconds": 245}
```

### Logs de Desarrollo
```
[2026-02-05 10:30:45.123 -05:00] [INF] [Namespace.ClassName] [correlation-id-123] Development message
{"Category": "Development", "DevLog": true, "ObjectData": {...}}
```

## Best Practices

### 1. Usa el nivel de log apropiado

- **Information**: Eventos normales del flujo de la aplicación
- **Warning**: Situaciones anormales pero recuperables
- **Error**: Errores que requieren atención
- **Fatal**: Errores críticos que pueden causar que la aplicación falle

### 2. Performance Logging

- Usa `LogPerformance` para operaciones que:
  - Llaman a servicios externos
  - Consultan bases de datos
  - Procesan grandes cantidades de datos
  - Operaciones críticas del negocio

### 3. Development Logging

- Los logs de desarrollo son ideales para:
  - Debugging en entornos de desarrollo
  - Trazar el flujo de ejecución
  - Inspeccionar valores de variables
  - Validar transformaciones de datos

### 4. No loggees información sensible

```csharp
// ❌ MAL
LoggingHelper.LogDevelopment("Password: {Password}", user.Password);

// ✅ BIEN
LoggingHelper.LogDevelopment("Usuario autenticado: {UserId}", user.Id);
```

### 5. Usa properties estructuradas

```csharp
// ❌ MAL
_logger.LogInformation($"User {userId} logged in at {DateTime.Now}");

// ✅ BIEN
_logger.LogInformation("Usuario {UserId} inició sesión a las {LoginTime}", userId, DateTime.UtcNow);
```

## Monitoreo y Análisis

### Ver logs en tiempo real

```bash
# Ver logs generales
tail -f logs/app-20260205.log

# Ver solo errores
tail -f logs/error/error-20260205.log

# Ver logs de performance
tail -f logs/performance/performance-20260205.log

# Ver logs de desarrollo
tail -f logs/dev/dev-20260205.log
```

### Buscar en logs

```bash
# Buscar errores de un usuario específico
grep "UserId: 123" logs/error/error-*.log

# Buscar operaciones lentas (más de 1000ms)
grep -E "completed in [0-9]{4,}ms" logs/performance/performance-*.log

# Buscar por correlation ID
grep "correlation-id-123" logs/app-*.log
```

## Integración con Application Insights (Opcional)

Si deseas enviar logs a Azure Application Insights, agrega el paquete:

```bash
dotnet add package Serilog.Sinks.ApplicationInsights
```

Y actualiza `appsettings.json`:

```json
{
  "Serilog": {
    "WriteTo": [
      {
        "Name": "ApplicationInsights",
        "Args": {
          "connectionString": "your-connection-string",
          "telemetryConverter": "Serilog.Sinks.ApplicationInsights.TelemetryConverters.TraceTelemetryConverter, Serilog.Sinks.ApplicationInsights"
        }
      }
    ]
  }
}
```

## Troubleshooting

### Los logs no se escriben

1. Verifica que las carpetas existen (se crean automáticamente al iniciar)
2. Verifica permisos de escritura en la carpeta `logs/`
3. Revisa la consola para errores de Serilog al iniciar

### Los logs de error/performance/dev no se generan

1. Verifica que instalaste `Serilog.Expressions`
2. Asegúrate de usar los métodos de `LoggingHelper` correctamente
3. Para logs de performance, usa la propiedad `Category = "Performance"`
4. Para logs de dev, usa `LoggingHelper.LogDevelopment()`

### Logs ocupan mucho espacio

Ajusta los `retainedFileCountLimit` en `appsettings.json` según tus necesidades.

## Resumen de Métodos

| Método | Carpeta de Destino | Uso |
|--------|-------------------|-----|
| `ILogger.LogInformation()` | `logs/` | Logs generales |
| `ILogger.LogWarning()` | `logs/` | Advertencias |
| `ILogger.LogError()` | `logs/` y `logs/error/` | Errores |
| `LoggingHelper.LogError()` | `logs/` y `logs/error/` | Errores con excepción |
| `LoggingHelper.LogFatal()` | `logs/` y `logs/error/` | Errores críticos |
| `LoggingHelper.LogPerformance()` | `logs/performance/` | Métricas de rendimiento |
| `LoggingHelper.LogDevelopment()` | `logs/dev/` | Logs de desarrollo |
| `LoggingHelper.LogDevelopmentWarning()` | `logs/dev/` | Warnings de desarrollo |
| `LoggingHelper.LogDevelopmentObject()` | `logs/dev/` | Objetos serializados |

---

**Documentación actualizada**: 2026-02-05
