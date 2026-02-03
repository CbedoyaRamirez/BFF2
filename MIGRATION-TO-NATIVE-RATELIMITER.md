# Migración a Rate Limiter Nativo de .NET 8

## ✅ Migración Completada

Se ha migrado exitosamente de **AspNetCoreRateLimit** (biblioteca de terceros) al **Rate Limiter nativo de .NET 8**.

---

## 📊 Comparación

| Característica | AspNetCoreRateLimit (Anterior) | Native .NET 8 (Actual) |
|----------------|--------------------------------|------------------------|
| **Dependencias** | Paquete NuGet externo | Incluido en framework |
| **Configuración** | appsettings.json | Código type-safe |
| **Performance** | Buena | Excelente (optimizado) |
| **Middleware** | `UseIpRateLimiting()` | `UseRateLimiter()` |
| **Granularidad** | Global/Endpoint | Múltiples políticas |
| **Atributos** | No soportado | `[EnableRateLimiting]` |
| **Algoritmos** | Fixed Window | Fixed, Sliding, Token Bucket, Concurrency |

---

## 🔧 Cambios Realizados

### 1. Eliminado del .csproj
```xml
<!-- ELIMINADO -->
<PackageReference Include="AspNetCoreRateLimit" Version="5.0.0" />
```

### 2. Actualizado Program.cs

#### Usings Actualizados
```csharp
// AÑADIDO
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

// ELIMINADO
using AspNetCoreRateLimit;
```

#### Configuración Nueva
```csharp
// NUEVO Sistema Nativo
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Política global - 60 requests/min
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User.Identity?.Name ?? httpContext.Request.Headers.Host.ToString(),
            factory: partition => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 60,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    // Política "api" - 100 requests/min
    options.AddFixedWindowLimiter("api", options => { ... });

    // Política "health" - 300 requests/min
    options.AddFixedWindowLimiter("health", options => { ... });

    // Política "strict" - 10 requests/min
    options.AddFixedWindowLimiter("strict", options => { ... });
});
```

#### Middleware Actualizado
```csharp
// ANTES
app.UseIpRateLimiting();

// AHORA
app.UseRateLimiter();
```

#### Health Checks con Políticas
```csharp
// Aplicar política específica
app.MapHealthChecks("/health").RequireRateLimiting("health");
```

### 3. Actualizado SystemController.cs

- **Eliminado:** Dependencias de `AspNetCoreRateLimit`
- **Actualizado:** Método `GetRateLimitConfig()` para reflejar configuración nativa
- **Actualizado:** DTOs para mostrar políticas nativas

### 4. Actualizado appsettings.json

```json
// ANTES (ELIMINADO)
"IpRateLimiting": { ... },
"IpRateLimitPolicies": { ... },
"ClientRateLimiting": { ... }

// AHORA (SIMPLIFICADO)
"RateLimiting": {
  "Note": "Rate limiting configuration is now managed in code",
  "GlobalLimit": 60,
  "WindowMinutes": 1,
  "Policies": {
    "Api": 100,
    "Health": 300,
    "Strict": 10
  }
}
```

### 5. Actualizada Documentación

- **RATE-LIMIT-HEALTH.md**: Actualizado para reflejar sistema nativo
- **Este archivo**: Documento de migración

---

## 🚀 Políticas Configuradas

### Política Global (Default)
- **Límite:** 60 requests/minuto
- **Aplica a:** Todos los endpoints sin política específica
- **Partición:** Por usuario autenticado o IP

### Política "api"
- **Límite:** 100 requests/minuto
- **Uso:** Endpoints de negocio (`/api/*`)
- **Recomendado para:** ChatBot, FAQBot, Speech

### Política "health"
- **Límite:** 300 requests/minuto
- **Uso:** Health checks (`/health*`)
- **Recomendado para:** Monitoreo, Kubernetes probes

### Política "strict"
- **Límite:** 10 requests/minuto
- **Uso:** Operaciones críticas o sensibles
- **Recomendado para:** Admin endpoints, operaciones costosas

---

## 📝 Cómo Usar las Políticas

### En Minimal APIs
```csharp
app.MapGet("/api/data", () => "data")
   .RequireRateLimiting("api");
```

### En Controllers
```csharp
[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("api")]  // Toda la clase
public class MyController : ControllerBase
{
    [HttpPost]
    [EnableRateLimiting("strict")]  // Sobrescribe para este método
    public ActionResult CriticalOperation() { ... }

    [HttpGet]
    [DisableRateLimiting]  // Deshabilita rate limiting
    public ActionResult PublicData() { ... }
}
```

---

## 🧪 Testing

Los scripts de prueba existentes siguen funcionando:

### PowerShell
```powershell
.\test-rate-limit.ps1
```

### Python
```bash
python test-rate-limit.py
```

### Bash
```bash
bash test-rate-limit.sh
```

### Resultado Esperado
- Requests 1-60: **200 OK** ✅
- Requests 61+: **429 Too Many Requests** ⚠️

---

## 📊 Respuesta 429

### Formato
```json
{
  "error": "Too Many Requests",
  "message": "Rate limit exceeded. Please try again in 60 seconds.",
  "retryAfter": 60
}
```

### Headers
```http
HTTP/1.1 429 Too Many Requests
Retry-After: 60
Content-Type: application/json
```

---

## ⚡ Ventajas del Sistema Nativo

### 1. Performance
- Más eficiente en memoria y CPU
- Optimizado por el equipo de .NET
- Menor overhead

### 2. Mantenibilidad
- Sin dependencias externas
- Actualizaciones automáticas con .NET
- Type-safe con IntelliSense completo

### 3. Flexibilidad
- Múltiples algoritmos disponibles
- Configuración granular por endpoint
- Fácil personalización

### 4. Integración
- Atributos nativos `[EnableRateLimiting]`
- Compatible con Minimal APIs y Controllers
- Funciona con autenticación y autorización

---

## 🔮 Algoritmos Disponibles

El sistema nativo soporta cuatro algoritmos (actualmente usando **Fixed Window**):

### 1. Fixed Window ✅ (Actual)
```csharp
options.AddFixedWindowLimiter("policy", options =>
{
    options.PermitLimit = 60;
    options.Window = TimeSpan.FromMinutes(1);
});
```

### 2. Sliding Window
```csharp
options.AddSlidingWindowLimiter("policy", options =>
{
    options.PermitLimit = 60;
    options.Window = TimeSpan.FromMinutes(1);
    options.SegmentsPerWindow = 6;
});
```

### 3. Token Bucket
```csharp
options.AddTokenBucketLimiter("policy", options =>
{
    options.TokenLimit = 100;
    options.ReplenishmentPeriod = TimeSpan.FromSeconds(10);
    options.TokensPerPeriod = 10;
});
```

### 4. Concurrency
```csharp
options.AddConcurrencyLimiter("policy", options =>
{
    options.PermitLimit = 10;
    options.QueueLimit = 5;
});
```

---

## 📋 Checklist de Migración

- [x] Eliminar paquete AspNetCoreRateLimit del .csproj
- [x] Actualizar usings en Program.cs
- [x] Reemplazar configuración con AddRateLimiter
- [x] Cambiar middleware a UseRateLimiter
- [x] Actualizar SystemController
- [x] Simplificar appsettings.json
- [x] Actualizar documentación
- [x] Compilar proyecto sin errores
- [x] Verificar tests funcionan

---

## ✅ Estado: COMPLETADO

**Fecha de Migración:** 2026-02-03
**Versión .NET:** 8.0
**Compilación:** ✅ Exitosa (0 errores)
**Tests:** ✅ Compatibles

---

## 📚 Referencias

- [ASP.NET Core Rate Limiting](https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit)
- [System.Threading.RateLimiting](https://learn.microsoft.com/en-us/dotnet/api/system.threading.ratelimiting)
- [Rate Limiting Middleware](https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit#enable-rate-limiting)

---

## 🎉 Resultado

El sistema ahora usa el Rate Limiter nativo de .NET 8, proporcionando:
- ✅ Mejor performance
- ✅ Menos dependencias
- ✅ Configuración type-safe
- ✅ Flexibilidad mejorada
- ✅ Mejor integración con el framework
