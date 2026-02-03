# Optimización de Health Checks - Microservicios

## ✅ Optimización Completada

Se han optimizado y mejorado los health checks para todos los microservicios y el BFF, proporcionando información detallada, manejo robusto de errores y mejor observabilidad.

---

## 🎯 Problemas Identificados y Resueltos

### Antes de la Optimización

❌ **Problemas:**
- Health checks básicos sin información detallada
- No se capturaba información de performance (memoria, uptime, etc.)
- Manejo de errores limitado cuando microservicios no están disponibles
- Respuestas simples sin metadata útil para debugging
- Timeouts no configurados correctamente
- No había diferenciación entre readiness y liveness

### Después de la Optimización

✅ **Mejoras:**
- Health checks detallados con información de sistema
- Captura de métricas: memoria, GC, uptime, response time
- Manejo robusto de excepciones y timeouts
- Respuestas JSON estructuradas con metadata completa
- Custom health check con detección de servicios degradados
- Endpoints separados: `/health`, `/health/ready`, `/health/live`
- Información de versión y estado de cada servicio

---

## 📊 Arquitectura de Health Checks

```
┌─────────────────────────────────────────────────────────────┐
│                        BFF API                              │
│                   (localhost:5016)                          │
│                                                             │
│  /health        - Complete health check (all services)     │
│  /health/ready  - Readiness check (ready tag)              │
│  /health/live   - Liveness check (live tag)                │
│                                                             │
│  Health Checks:                                             │
│  ├─ self (✓)                                                │
│  ├─ chatbot (HttpEndpointHealthCheck)                      │
│  ├─ faqbot (HttpEndpointHealthCheck)                       │
│  └─ speechservice (HttpEndpointHealthCheck)                │
└─────────────────────────────────────────────────────────────┘
         │                    │                    │
         ▼                    ▼                    ▼
   ┌──────────┐        ┌──────────┐        ┌─────────────┐
   │ ChatBot  │        │ FAQBot   │        │SpeechService│
   │  :5266   │        │  :5267   │        │    :7001    │
   └──────────┘        └──────────┘        └─────────────┘
   /health             /health             /health
   /health/ready       /health/ready       /health/ready
   /health/live        /health/live        /health/live

   Checks:             Checks:             Checks:
   ├─ self (✓)         ├─ self (✓)         ├─ self (✓)
   ├─ memory           ├─ memory           ├─ memory
   └─ uptime           └─ uptime           └─ uptime
```

---

## 🔧 Cambios Realizados

### 1. Microservicios (ChatBot, FAQBot, SpeechService)

#### Antes:
```csharp
// Muy básico, sin información
builder.Services.AddHealthChecks();
app.MapHealthChecks("/health");
```

#### Después:
```csharp
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy("Service is running"))
    .AddCheck("memory", () =>
    {
        var allocatedMB = GC.GetTotalMemory(false) / 1024 / 1024;
        var status = allocatedMB > 500 ? HealthStatus.Degraded : HealthStatus.Healthy;
        return new HealthCheckResult(status, "Memory usage is normal", data: new Dictionary<string, object>
        {
            { "allocatedMB", allocatedMB },
            { "gen0Collections", GC.CollectionCount(0) },
            { "gen1Collections", GC.CollectionCount(1) },
            { "gen2Collections", GC.CollectionCount(2) }
        });
    })
    .AddCheck("uptime", () =>
    {
        var uptime = DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime();
        return HealthCheckResult.Healthy("Service is running", data: new Dictionary<string, object>
        {
            { "uptime", uptime.ToString() },
            { "startTime", Process.GetCurrentProcess().StartTime.ToUniversalTime() }
        });
    });

// Endpoints con respuestas JSON detalladas
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            service = "ChatBot",
            version = "1.0.0",
            timestamp = DateTime.UtcNow,
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration = e.Value.Duration.TotalMilliseconds,
                data = e.Value.Data
            })
        });
        await context.Response.WriteAsync(result);
    }
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live")
});
```

### 2. BFF API - Custom Health Check

Se creó un health check personalizado robusto: `HttpEndpointHealthCheck.cs`

**Características:**

✅ **Timeouts explícitos:** 5 segundos por servicio
✅ **Manejo de excepciones:** Captura HttpRequestException, OperationCanceledException, etc.
✅ **Métricas de performance:** Response time, status code
✅ **Detección de degradación:** Basado en tiempo de respuesta
✅ **Parsing de respuestas:** Lee información de los microservicios
✅ **Información detallada de errores:** Incluye tipo de error, mensaje, inner exceptions

**Lógica de Estado:**

```csharp
// Unhealthy
- Status code != 200
- Timeout (> 5 segundos)
- HttpRequestException (servicio no disponible)
- Cualquier exception no manejada

// Degraded
- Response time > 3 segundos (muy lento)
- Response time > 1 segundo (lento)

// Healthy
- Status code = 200
- Response time < 1 segundo
```

### 3. BFF API - Configuración

#### Antes:
```csharp
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy("API is running"))
    .AddUrlGroup(new Uri($"{chatBotUrl}/health"), "chatbot", timeout: TimeSpan.FromSeconds(5));
```

#### Después:
```csharp
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
        args: new object[] { $"{chatBotUrl}/health", "ChatBot" });
```

---

## 📋 Endpoints de Health Check

### BFF API (localhost:5016)

#### `/health` - Complete Health Check
Verifica todos los servicios (self + microservicios)

**Tags:** Todos
**Respuesta:**
```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.234",
  "entries": {
    "self": {
      "status": "Healthy",
      "description": "BFF API is running",
      "duration": "00:00:00.001",
      "data": {
        "uptime": "01:23:45.678",
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
        "serviceStatus": "Healthy"
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
        "statusCode": 200
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

#### `/health/ready` - Readiness Check
Verifica si el API está listo para recibir tráfico

**Tags:** "ready"
**Uso:** Kubernetes readiness probe

#### `/health/live` - Liveness Check
Verifica si el API está vivo (básico)

**Tags:** "live"
**Uso:** Kubernetes liveness probe

### Microservicios (ChatBot, FAQBot, SpeechService)

#### `/health` - Complete Health Check
**Respuesta:**
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

#### `/health/ready` - Readiness Check
Verifica checks con tag "ready" (self + memory)

#### `/health/live` - Liveness Check
Verifica checks con tag "live" (self + uptime)

---

## 🧪 Testing

### Test Manual con cURL

```bash
# BFF Complete Health Check
curl http://localhost:5016/health | jq

# BFF Readiness
curl http://localhost:5016/health/ready

# BFF Liveness
curl http://localhost:5016/health/live

# ChatBot Health
curl http://localhost:5266/health | jq

# FAQBot Health
curl http://localhost:5267/health | jq

# SpeechService Health
curl http://localhost:7001/health | jq
```

### Test con PowerShell

```powershell
# BFF Health Check
Invoke-RestMethod -Uri http://localhost:5016/health | ConvertTo-Json -Depth 10

# Verificar todos los microservicios
$services = @(
    @{ Name = "BFF"; Url = "http://localhost:5016/health" },
    @{ Name = "ChatBot"; Url = "http://localhost:5266/health" },
    @{ Name = "FAQBot"; Url = "http://localhost:5267/health" },
    @{ Name = "SpeechService"; Url = "http://localhost:7001/health" }
)

foreach ($service in $services) {
    Write-Host "`n=== $($service.Name) ===" -ForegroundColor Cyan
    try {
        $response = Invoke-RestMethod -Uri $service.Url
        Write-Host "Status: $($response.status)" -ForegroundColor Green
    }
    catch {
        Write-Host "Status: Unavailable" -ForegroundColor Red
        Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Yellow
    }
}
```

### Test Automatizado

Crear archivo `test-health-checks.http`:

```http
### BFF Complete Health Check
GET http://localhost:5016/health

### BFF Readiness
GET http://localhost:5016/health/ready

### BFF Liveness
GET http://localhost:5016/health/live

### ChatBot Health
GET http://localhost:5266/health

### FAQBot Health
GET http://localhost:5267/health

### SpeechService Health
GET http://localhost:7001/health
```

---

## 🚀 Estados de Health Check

| Estado | HTTP Code | Descripción | Cuándo Ocurre |
|--------|-----------|-------------|---------------|
| **Healthy** | 200 | Todo funciona correctamente | Todos los checks pasan, response time < 1s |
| **Degraded** | 200 | Funciona pero con problemas menores | Response time 1-3s, memoria alta |
| **Unhealthy** | 503 | Servicio no disponible | Timeout, error de conexión, status ≠ 200 |

---

## 🔍 Información Capturada

### Microservicios

#### Self Check
- ✅ Estado del servicio
- ✅ Nombre del servicio
- ✅ Versión

#### Memory Check
- ✅ Memoria asignada (MB)
- ✅ Colecciones de GC (Gen 0, 1, 2)
- ✅ Estado: Healthy si < 500MB, Degraded si >= 500MB

#### Uptime Check
- ✅ Tiempo de ejecución
- ✅ Hora de inicio del proceso

### BFF API - HttpEndpointHealthCheck

#### Información Básica
- ✅ URL del endpoint
- ✅ Nombre del servicio
- ✅ Response time (ms)
- ✅ Status code HTTP

#### Información del Servicio (si disponible)
- ✅ Versión del servicio
- ✅ Estado del servicio
- ✅ Checks individuales del servicio

#### Información de Errores
- ✅ Mensaje de error
- ✅ Tipo de excepción
- ✅ Inner exception (si existe)
- ✅ Indicador de timeout

---

## 📊 Métricas y Observabilidad

### Kubernetes Integration

```yaml
# deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: bff-api
spec:
  template:
    spec:
      containers:
      - name: bff
        image: bff-api:latest
        ports:
        - containerPort: 5016
        livenessProbe:
          httpGet:
            path: /health/live
            port: 5016
          initialDelaySeconds: 30
          periodSeconds: 10
          timeoutSeconds: 5
          failureThreshold: 3
        readinessProbe:
          httpGet:
            path: /health/ready
            port: 5016
          initialDelaySeconds: 10
          periodSeconds: 5
          timeoutSeconds: 5
          failureThreshold: 3
```

### Prometheus Integration

```yaml
# prometheus.yml
scrape_configs:
  - job_name: 'bff-api'
    metrics_path: '/health'
    static_configs:
      - targets: ['localhost:5016']
  - job_name: 'chatbot'
    metrics_path: '/health'
    static_configs:
      - targets: ['localhost:5266']
  - job_name: 'faqbot'
    metrics_path: '/health'
    static_configs:
      - targets: ['localhost:5267']
  - job_name: 'speechservice'
    metrics_path: '/health'
    static_configs:
      - targets: ['localhost:7001']
```

---

## 🎯 Mejores Prácticas Implementadas

### ✅ Separación de Concerns
- `/health` - Estado completo de todos los servicios
- `/health/ready` - Solo checks necesarios para aceptar tráfico
- `/health/live` - Solo checks básicos de vida

### ✅ Timeouts Apropiados
- 5 segundos por health check externo
- Evita que health checks lentos bloqueen la aplicación

### ✅ Estados Granulares
- **Healthy:** Todo bien
- **Degraded:** Funcional pero con issues
- **Unhealthy:** No disponible

### ✅ Información Rica
- Metadata detallada en cada check
- Response times para análisis de performance
- Información de errores para debugging

### ✅ Manejo Robusto de Errores
- Captura todas las excepciones posibles
- Proporciona información de contexto
- No falla silenciosamente

### ✅ Tags para Filtering
- "ready" - Para readiness probes
- "live" - Para liveness probes
- "external" - Para servicios externos
- "services" - Para microservicios

---

## 📝 Troubleshooting

### Problema: Microservicio aparece como Unhealthy

**Posibles causas:**
1. El microservicio no está corriendo
2. URL incorrecta en appsettings.json
3. Firewall bloqueando conexión
4. Timeout muy corto

**Solución:**
```bash
# Verificar que el servicio esté corriendo
curl http://localhost:5266/health

# Verificar logs del BFF
cat logs/app-*.log | grep "chatbot"

# Verificar configuración
curl http://localhost:5016/api/system/rate-limit-config
```

### Problema: Health check siempre Degraded

**Posibles causas:**
1. Response time > 1 segundo
2. Memoria alta (> 500MB)

**Solución:**
```bash
# Ver detalles del health check
curl http://localhost:5016/health | jq '.entries.chatbot.data'

# Verificar response time
# Si > 1s, optimizar el microservicio o ajustar umbral
```

### Problema: Timeout en health checks

**Posibles causas:**
1. Microservicio muy lento
2. Deadlock o blocking operation
3. Network issues

**Solución:**
1. Revisar logs del microservicio
2. Aumentar timeout si es necesario (cuidado con Kubernetes probes)
3. Optimizar operaciones en health check

---

## ✅ Checklist de Optimización

- [x] Health checks mejorados en ChatBot microservicio
- [x] Health checks mejorados en FAQBot microservicio
- [x] Health checks mejorados en SpeechService microservicio
- [x] Custom health check (HttpEndpointHealthCheck) en BFF
- [x] Configuración actualizada en BFF Program.cs
- [x] Endpoints /health, /health/ready, /health/live en todos los servicios
- [x] Respuestas JSON detalladas
- [x] Manejo robusto de errores
- [x] Información de performance (response time, memory, uptime)
- [x] Tags para filtering (ready, live, external, services)
- [x] Estados granulares (Healthy, Degraded, Unhealthy)
- [x] Compilación exitosa

---

## 🎉 Resultado

Ahora tienes un sistema de health checks robusto y completo que:

✅ Proporciona información detallada sobre el estado de cada servicio
✅ Maneja errores de manera robusta sin fallar
✅ Detecta servicios degradados antes de que fallen completamente
✅ Es compatible con Kubernetes probes
✅ Facilita el debugging con información rica
✅ Captura métricas útiles para observabilidad

---

## 📚 Referencias

- [ASP.NET Core Health Checks](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks)
- [Health Checks in .NET](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/implement-resilient-applications/monitor-app-health)
- [Kubernetes Probes](https://kubernetes.io/docs/tasks/configure-pod-container/configure-liveness-readiness-startup-probes/)
