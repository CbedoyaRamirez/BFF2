# Rate Limiting y Health Checks - BFF API

Este documento describe la implementación de Rate Limiting y Health Checks en el BFF de Chubb Bot AI Assistant.

## 📋 Tabla de Contenidos

- [Rate Limiting](#rate-limiting)
- [Health Checks](#health-checks)
- [Endpoints del Sistema](#endpoints-del-sistema)
- [Testing](#testing)

---

## 🚦 Rate Limiting

### ⚡ Sistema Nativo de .NET 8

El API utiliza el **Rate Limiter nativo de .NET 8**, integrado directamente en ASP.NET Core. Este sistema es más eficiente, performante y no requiere dependencias externas.

### Características

- ✅ **Integrado en .NET 8** - Sin dependencias de terceros
- ✅ **Algoritmo Fixed Window** - Ventana fija de tiempo
- ✅ **Múltiples Políticas** - Configuración granular por endpoint
- ✅ **Type-Safe** - Configuración en código con IntelliSense
- ✅ **Alto Rendimiento** - Optimizado por Microsoft

### Políticas Configuradas

#### 1. Política Global (Predeterminada)

Se aplica a todos los endpoints que no tengan una política específica:

- **Límite:** 60 requests por minuto
- **Algoritmo:** Fixed Window
- **Partición:** Por IP o usuario autenticado
- **Queue:** 0 (sin cola)

#### 2. Política "api"

Para endpoints de API (`/api/*`):

- **Límite:** 100 requests por minuto
- **Uso:** Endpoints de negocio (Chat, FAQ, Speech)

#### 3. Política "health"

Para health checks (`/health*`):

- **Límite:** 300 requests por minuto
- **Uso:** Monitoreo y health checks

#### 4. Política "strict"

Para operaciones críticas:

- **Límite:** 10 requests por minuto
- **Uso:** Operaciones sensibles

### Respuesta cuando se excede el límite

```http
HTTP/1.1 429 Too Many Requests
Retry-After: 60
Content-Type: application/json

{
  "error": "Too Many Requests",
  "message": "Rate limit exceeded. Please try again in 60 seconds.",
  "retryAfter": 60
}
```

### Configuración en Código

```csharp
// Program.cs - Configuración
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Política global
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User.Identity?.Name ?? httpContext.Request.Headers.Host.ToString(),
            factory: partition => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 60,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    // Política "api"
    options.AddFixedWindowLimiter("api", options =>
    {
        options.PermitLimit = 100;
        options.Window = TimeSpan.FromMinutes(1);
    });

    // Respuesta personalizada
    options.OnRejected = async (context, token) =>
    {
        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            error = "Too Many Requests",
            message = $"Rate limit exceeded. Please try again in {retryAfter} seconds."
        }, cancellationToken: token);
    };
});

// Program.cs - Middleware
app.UseRateLimiter();

// Aplicar política a endpoints específicos
app.MapHealthChecks("/health").RequireRateLimiting("health");
```

### Aplicar Políticas a Controllers

Usa el atributo `[EnableRateLimiting]` en controllers:

```csharp
[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("api")]  // Aplica política "api"
public class ChatController : ControllerBase
{
    [HttpPost]
    [EnableRateLimiting("strict")]  // Sobrescribe con política "strict"
    public async Task<ActionResult> CriticalOperation()
    {
        // ...
    }

    [HttpGet]
    [DisableRateLimiting]  // Deshabilita rate limiting
    public ActionResult GetPublicInfo()
    {
        // ...
    }
}
```

---

## 🏥 Health Checks

⚡ **Sistema Optimizado con Custom Health Checks**

El BFF y todos los microservicios tienen health checks mejorados con información detallada, manejo robusto de errores y métricas de performance.

> 📋 **Ver documentación completa:** [HEALTH-CHECKS-OPTIMIZATION.md](HEALTH-CHECKS-OPTIMIZATION.md)

### Endpoints Disponibles

Todos los servicios (BFF, ChatBot, FAQBot, SpeechService) proporcionan:

- **`/health`** - Health check completo con información detallada
- **`/health/ready`** - Readiness check (para Kubernetes)
- **`/health/live`** - Liveness check (para Kubernetes)

### BFF API - Health Check Completo

Verifica el estado de **todos los servicios** usando custom health check:
- ✅ Self (BFF API)
- ✅ ChatBot Service
- ✅ FAQBot Service
- ✅ SpeechService

**Características:**
- ✅ Timeout de 5 segundos por servicio
- ✅ Detección de servicios degradados (response time > 1s)
- ✅ Manejo robusto de errores y timeouts
- ✅ Información detallada de cada servicio
- ✅ Parsing de respuestas de microservicios

**Ejemplo de Respuesta:**

```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.456",
  "entries": {
    "self": {
      "status": "Healthy",
      "description": "BFF API is running",
      "duration": "00:00:00.001",
      "data": {
        "uptime": "02:15:30",
        "memoryUsageMB": 125
      }
    },
    "chatbot": {
      "status": "Healthy",
      "description": "ChatBot is responding normally",
      "duration": "00:00:00.123",
      "data": {
        "url": "http://localhost:5266/health",
        "service": "ChatBot",
        "responseTime": "123ms",
        "statusCode": 200,
        "serviceVersion": "1.0.0",
        "serviceStatus": "Healthy",
        "serviceChecks": "self: Healthy, memory: Healthy, uptime: Healthy"
      }
    },
    "faqbot": {
      "status": "Degraded",
      "description": "FAQBot response time is elevated (1234ms)",
      "duration": "00:00:01.234",
      "data": {
        "url": "http://localhost:5267/health",
        "service": "FAQBot",
        "responseTime": "1234ms",
        "statusCode": 200,
        "serviceVersion": "1.0.0"
      }
    },
    "speechservice": {
      "status": "Unhealthy",
      "description": "SpeechService is unavailable: No connection could be made",
      "duration": "00:00:05.000",
      "data": {
        "url": "http://localhost:7001/health",
        "service": "SpeechService",
        "responseTime": "5000ms (timeout)",
        "error": "No connection could be made",
        "errorType": "HttpRequestException"
      }
    }
  }
}
```

### Microservicios - Health Check Detallado

Cada microservicio proporciona información sobre:

**Checks Incluidos:**
- ✅ **Self** - Estado del servicio
- ✅ **Memory** - Uso de memoria y colecciones de GC
- ✅ **Uptime** - Tiempo de ejecución

**Ejemplo de Respuesta de ChatBot:**

```json
{
  "status": "Healthy",
  "service": "ChatBot",
  "version": "1.0.0",
  "timestamp": "2026-02-03T12:00:00Z",
  "checks": [
    {
      "name": "self",
      "status": "Healthy",
      "description": "ChatBot API is running",
      "duration": 0.123,
      "data": {}
    },
    {
      "name": "memory",
      "status": "Healthy",
      "description": "Memory usage is normal",
      "duration": 0.045,
      "data": {
        "allocatedMB": 128,
        "gen0Collections": 5,
        "gen1Collections": 2,
        "gen2Collections": 0
      }
    },
    {
      "name": "uptime",
      "status": "Healthy",
      "description": "Service is running",
      "duration": 0.012,
      "data": {
        "uptime": "02:15:30",
        "startTime": "2026-02-03T09:44:30Z"
      }
    }
  ]
}
```

### Estados de Health Check

| Estado | HTTP Code | Descripción | Cuándo Ocurre |
|--------|-----------|-------------|---------------|
| **Healthy** | 200 | Todo funciona correctamente | Response time < 1s, todos los checks pasan |
| **Degraded** | 200 | Funciona pero con issues | Response time 1-3s, memoria alta (>500MB) |
| **Unhealthy** | 503 | Servicio no disponible | Timeout (>5s), error de conexión, status ≠ 200 |

### Custom Health Check - HttpEndpointHealthCheck

El BFF usa un custom health check optimizado para verificar microservicios:

**Características:**
- ✅ Timeout explícito de 5 segundos
- ✅ Captura de todas las excepciones (HttpRequestException, OperationCanceledException, etc.)
- ✅ Métricas de performance (response time, status code)
- ✅ Parsing de respuestas JSON de microservicios
- ✅ Detección automática de servicios degradados
- ✅ Información detallada de errores para debugging

**Lógica de Estado:**

```csharp
// Unhealthy
- Status code != 200
- Timeout (> 5 segundos)
- HttpRequestException (servicio no disponible)

// Degraded
- Response time > 3 segundos (muy lento)
- Response time > 1 segundo (lento)

// Healthy
- Status code = 200
- Response time < 1 segundo
```

### Configuración en Código (BFF)

```csharp
// Program.cs
builder.Services.AddHealthChecks()
    .AddCheck("self", () =>
    {
        var process = Process.GetCurrentProcess();
        var uptime = DateTime.UtcNow - process.StartTime.ToUniversalTime();
        var memoryMB = process.WorkingSet64 / 1024 / 1024;

        return HealthCheckResult.Healthy("BFF API is running", data: new Dictionary<string, object>
        {
            { "uptime", uptime.ToString() },
            { "memoryUsageMB", memoryMB }
        });
    }, tags: new[] { "ready", "live" })
    .AddTypeActivatedCheck<HttpEndpointHealthCheck>(
        "chatbot",
        failureStatus: HealthStatus.Degraded,
        tags: new[] { "external", "services" },
        args: new object[] { $"{chatBotUrl}/health", "ChatBot" })
    .AddUrlGroup(
        new Uri($"{faqBotBaseUrl}/health"),
        name: "faqbot",
        failureStatus: HealthStatus.Degraded,
        tags: new[] { "external", "services" },
        timeout: TimeSpan.FromSeconds(5))
    .AddUrlGroup(
        new Uri($"{speechServiceBaseUrl}/health"),
        name: "speechservice",
        failureStatus: HealthStatus.Degraded,
        tags: new[] { "external", "services" },
        timeout: TimeSpan.FromSeconds(5));

// Endpoints
app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});
```

---

## 🔧 Endpoints del Sistema

El API incluye endpoints adicionales para obtener información y configuración:

### 1. `/api/system/info` - Información del Sistema

Retorna información general del sistema:

```json
{
  "applicationName": "Chubb Bot AI Assistant API",
  "version": "1.0.0",
  "environment": "Development",
  "machineName": "SERVER-01",
  "osVersion": "Microsoft Windows NT 10.0.19045.0",
  "processorCount": 8,
  "upTime": "02:15:30",
  "timestamp": "2024-01-01T12:00:00Z"
}
```

### 2. `/api/system/rate-limit-config` - Configuración de Rate Limiting

Retorna la configuración actual de rate limiting:

```json
{
  "ipRateLimiting": {
    "enabled": true,
    "httpStatusCode": 429,
    "realIpHeader": "X-Real-IP",
    "clientIdHeader": "X-ClientId",
    "generalRules": [
      {
        "endpoint": "*",
        "period": "1m",
        "limit": 60
      },
      {
        "endpoint": "*",
        "period": "1h",
        "limit": 1000
      }
    ]
  },
  "clientRateLimiting": {
    "enabled": true,
    "httpStatusCode": 429,
    "clientIdHeader": "X-ClientId",
    "generalRules": [
      {
        "endpoint": "*",
        "period": "1s",
        "limit": 10
      },
      {
        "endpoint": "*",
        "period": "1m",
        "limit": 100
      }
    ]
  }
}
```

### 3. `/api/system/endpoints` - Lista de Endpoints

Retorna todos los endpoints disponibles organizados por categoría:

```json
{
  "health": [
    {
      "path": "/health",
      "description": "Health check completo de todos los servicios",
      "method": "GET"
    }
  ],
  "chat": [
    {
      "path": "/api/chat",
      "description": "Enviar mensaje al ChatBot",
      "method": "POST"
    }
  ],
  "faq": [...],
  "speech": [...],
  "system": [...]
}
```

---

## 🧪 Testing

### Testing Rate Limiting

#### Opción 1: Archivo .http (VS Code / Visual Studio)

Usa el archivo `health-checks.http` incluido en el proyecto:

```http
### Test Rate Limit - Ejecuta este request más de 60 veces rápidamente
GET http://localhost:5016/health
```

#### Opción 2: Script PowerShell

```powershell
# Test Rate Limiting - Enviar 100 requests
for ($i = 1; $i -le 100; $i++) {
    Write-Host "Request $i"
    Invoke-RestMethod -Uri "http://localhost:5016/health" -Method Get
    Start-Sleep -Milliseconds 100
}
```

#### Opción 3: cURL

```bash
# Enviar múltiples requests en loop
for i in {1..100}; do
  echo "Request $i"
  curl -s http://localhost:5016/health
  sleep 0.1
done
```

**Resultado Esperado:**

- Primeros 60 requests: **200 OK**
- Requests 61+: **429 Too Many Requests**

### Testing Health Checks

#### Opción 1: Script Automatizado (PowerShell) ⭐ RECOMENDADO

Usa el script `test-health-checks.ps1` que verifica todos los servicios:

```powershell
.\test-health-checks.ps1
```

**Características:**
- ✅ Verifica BFF, ChatBot, FAQBot y SpeechService
- ✅ Muestra estado con colores (Verde=Healthy, Amarillo=Degraded, Rojo=Unhealthy)
- ✅ Captura response time de cada servicio
- ✅ Muestra checks individuales de cada microservicio
- ✅ Muestra resumen con tabla de resultados
- ✅ Maneja errores cuando servicios no están disponibles

**Salida Ejemplo:**
```
==========================================
  HEALTH CHECK - TODOS LOS SERVICIOS
==========================================

Verificando BFF API...
  ✓ Status: Healthy
  ⏱ Response Time: 234ms
  External Services:
    ✓ chatbot: Healthy
       Response Time: 123ms
    ⚠ faqbot: Degraded
       Response Time: 1234ms
    ✗ speechservice: Unhealthy

Verificando ChatBot...
  ✓ Status: Healthy
  ⏱ Response Time: 123ms
  📦 Service: ChatBot
  🏷 Version: 1.0.0
  Checks:
    ✓ self: Healthy
    ✓ memory: Healthy
    ✓ uptime: Healthy

==========================================
  RESUMEN
==========================================
Total Services:      4
✓ Healthy:           2
⚠ Degraded:          1
✗ Unavailable:       1
```

#### Opción 2: Archivo .http (VS Code / Visual Studio)

Usa el archivo `test-health-checks.http` incluido en el proyecto:

```http
### BFF Complete Health Check
GET http://localhost:5016/health

### ChatBot Complete Health Check
GET http://localhost:5266/health

### FAQBot Complete Health Check
GET http://localhost:5267/health

### SpeechService Complete Health Check
GET http://localhost:7001/health
```

#### Opción 3: cURL

```bash
# Health check completo BFF
curl http://localhost:5016/health | jq

# Health check completo ChatBot
curl http://localhost:5266/health | jq

# Readiness check
curl http://localhost:5016/health/ready

# Liveness check
curl http://localhost:5016/health/live
```

### Verificar Configuración

```bash
# Ver configuración de Rate Limiting
curl http://localhost:5016/api/system/rate-limit-config

# Ver información del sistema
curl http://localhost:5016/api/system/info

# Ver todos los endpoints
curl http://localhost:5016/api/system/endpoints
```

---

## 📊 Monitoreo y Alertas

### Métricas Recomendadas

1. **Rate Limiting**
   - Número de requests bloqueados por minuto/hora
   - IPs más bloqueadas
   - Endpoints más afectados

2. **Health Checks**
   - Uptime de servicios externos
   - Latencia de health checks
   - Frecuencia de estados Degraded/Unhealthy

### Integración con Prometheus

El health check endpoint `/health` puede ser consumido por Prometheus para monitoreo:

```yaml
scrape_configs:
  - job_name: 'bff-api'
    metrics_path: '/health'
    static_configs:
      - targets: ['localhost:5016']
```

---

## 🔒 Seguridad

### Buenas Prácticas Implementadas

1. ✅ Rate Limiting por IP y por Cliente
2. ✅ Headers de identificación (X-ClientId, X-Real-IP)
3. ✅ Timeouts en health checks (5 segundos)
4. ✅ Respuestas detalladas solo en Development
5. ✅ Configuración separada por ambiente

### Recomendaciones

1. **Producción**: Ajustar límites según carga esperada
2. **Logging**: Monitorear requests bloqueados
3. **Alertas**: Configurar alertas cuando servicios estén Unhealthy
4. **Proxies**: Configurar X-Real-IP header correctamente

---

## 📝 Notas Importantes

1. **Redis está comentado temporalmente**: Los health checks de Redis no están activos
2. **Rate Limiting usa memoria**: Los límites son por instancia de la aplicación
3. **Health checks tienen timeout**: 5 segundos máximo por servicio
4. **Desarrollo vs Producción**: Límites más permisivos en desarrollo

---

## 🚀 Próximos Pasos

- [ ] Implementar rate limiting distribuido con Redis
- [ ] Agregar métricas de Prometheus
- [ ] Dashboard de monitoreo con Grafana
- [ ] Alertas automáticas en Slack/Teams
- [ ] Rate limiting por endpoint específico
- [ ] Rate limiting por roles/usuarios

---

## 📞 Soporte

Para más información sobre la configuración o troubleshooting:
- Ver logs en: `logs/app-YYYYMMDD.log`
- Swagger UI: http://localhost:5016/swagger
- Health Check: http://localhost:5016/health
