# Funcionalidades Implementadas - BFF API

Este documento describe las funcionalidades avanzadas implementadas en el BFF (Backend For Frontend) de Chubb Bot AI Assistant.

## 1. Gestión de Sesiones con Redis

### Características
- Almacenamiento de sesiones distribuido usando Redis
- Gestión automática de expiración de sesiones
- Soporte para múltiples sesiones por usuario
- Extensión de tiempo de vida de sesiones

### Servicio
- **Interface**: `ISessionService`
- **Implementación**: `SessionService`

### Métodos Disponibles
```csharp
Task<Session?> GetSessionAsync(string sessionId)
Task<Session> CreateSessionAsync(string userId, Dictionary<string, string>? metadata = null)
Task UpdateSessionAsync(Session session)
Task<bool> DeleteSessionAsync(string sessionId)
Task<bool> ValidateSessionAsync(string sessionId)
Task<IEnumerable<Session>> GetUserSessionsAsync(string userId)
Task ExtendSessionAsync(string sessionId, int additionalMinutes)
```

### Configuración
```json
"RedisSettings": {
  "ConnectionString": "localhost:6379",
  "InstanceName": "BFF:",
  "DefaultTTLMinutes": 30
}
```

## 2. Rate Limiting y Throttling

### Características
- Limitación de peticiones por IP
- Limitación de peticiones por cliente
- Configuración granular por endpoint
- Reglas personalizables por período de tiempo

### Configuración

#### Rate Limiting por IP
```json
"IpRateLimiting": {
  "EnableEndpointRateLimiting": true,
  "GeneralRules": [
    {
      "Endpoint": "*",
      "Period": "1m",
      "Limit": 60
    },
    {
      "Endpoint": "*",
      "Period": "1h",
      "Limit": 1000
    }
  ]
}
```

#### Rate Limiting por Cliente
```json
"ClientRateLimiting": {
  "EnableEndpointRateLimiting": true,
  "ClientIdHeader": "X-ClientId",
  "HttpStatusCode": 429,
  "GeneralRules": [
    {
      "Endpoint": "*",
      "Period": "1s",
      "Limit": 10
    }
  ]
}
```

### Respuesta cuando se excede el límite
- **Status Code**: 429 Too Many Requests
- **Headers**: `Retry-After` con el tiempo de espera

## 3. Polly - Resiliencia

### Políticas Implementadas
1. **Retry Policy**: Reintentos automáticos con backoff exponencial
2. **Circuit Breaker**: Protección contra fallos en cascada
3. **Timeout Policy**: Límites de tiempo para requests

### Características
- Logging detallado de cada evento (retry, circuit breaker abierto/cerrado)
- Configuración personalizada por servicio
- Integración con Serilog para observabilidad

### Configuración por Servicio
```json
"HttpClients": {
  "FAQBot": {
    "BaseUrl": "http://localhost:5267",
    "TimeoutSeconds": 10,
    "RetryCount": 3,
    "CircuitBreakerThreshold": 5,
    "CircuitBreakerDurationSeconds": 30
  }
}
```

### Eventos de Polly
- **Retry**: Log de advertencia con número de intento y razón
- **Circuit Breaker Abierto**: Log de error cuando el circuit breaker se abre
- **Circuit Breaker Cerrado**: Log de información cuando se recupera el servicio
- **Timeout**: Log de advertencia cuando un request excede el tiempo límite

## 4. Validación y Manejo de Errores

### Características
- Manejo centralizado de excepciones
- Respuestas de error estandarizadas
- Validación con FluentValidation
- Enriquecimiento de contexto con Serilog
- Información detallada en desarrollo, segura en producción

### Tipos de Errores Manejados
1. **ValidationException**: Errores de validación (400 Bad Request)
2. **BusinessException**: Errores de lógica de negocio (código personalizado)
3. **UnauthorizedAccessException**: Acceso no autorizado (401 Unauthorized)
4. **TaskCanceledException**: Timeout de request (408 Request Timeout)
5. **Exception**: Errores no controlados (500 Internal Server Error)

### Formato de Respuesta de Error
```json
{
  "errorCode": "ERROR_CODE",
  "message": "Descripción del error",
  "timestamp": "2024-01-01T12:00:00Z",
  "traceId": "correlation-id-guid",
  "details": ["Detalle adicional 1", "Detalle adicional 2"]
}
```

### Propiedades del ErrorResponse
- **errorCode**: Código único del error
- **message**: Mensaje descriptivo
- **timestamp**: Fecha y hora del error
- **traceId**: ID de correlación para tracking
- **details**: Lista opcional de detalles (solo en desarrollo)

## 5. Health Checks Detallados

### Endpoints
- `/health` - Estado completo de todos los servicios
- `/health/ready` - Verificación de disponibilidad (liveness)
- `/health/live` - Verificación básica de vida

### Servicios Monitoreados
1. **Redis**: Latencia, conexiones activas, estadísticas
2. **QuoteBot**: Disponibilidad y tiempo de respuesta
3. **FAQBot**: Disponibilidad y tiempo de respuesta
4. **SpeechService**: Disponibilidad y tiempo de respuesta

### Información Incluida en Health Checks
- Estado del servicio (Healthy, Degraded, Unhealthy)
- Tiempo de respuesta en milisegundos
- Información adicional del servicio
- Latencia de Redis y estadísticas de conexión

### Ejemplo de Respuesta
```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.1234567",
  "entries": {
    "redis": {
      "status": "Healthy",
      "description": "Redis is healthy (latency: 5.23ms)",
      "data": {
        "latency": "5.23ms",
        "connectedClients": "10",
        "endpoint": "localhost:6379"
      }
    },
    "faqbot": {
      "status": "Healthy",
      "data": {
        "responseTime": "125ms",
        "statusCode": 200
      }
    }
  }
}
```

## 6. Observabilidad y Logging con Serilog

### Características
- Logging estructurado
- Enriquecimiento automático de logs
- Múltiples destinos (Console, File)
- Rotación automática de archivos
- Request logging con información detallada

### Información Capturada
- Timestamp con zona horaria
- Nivel de log (Debug, Info, Warning, Error, Fatal)
- Contexto de la aplicación
- Correlation ID para tracking de requests
- Información de la request (método, path, status code, tiempo de respuesta)
- Machine name y thread ID
- User Agent y host

### Configuración de Sinks
```json
"Serilog": {
  "WriteTo": [
    {
      "Name": "Console",
      "Args": {
        "outputTemplate": "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level:u3}] {Message:lj}{NewLine}{Exception}"
      }
    },
    {
      "Name": "File",
      "Args": {
        "path": "logs/app-.log",
        "rollingInterval": "Day",
        "retainedFileCountLimit": 30
      }
    }
  ],
  "Enrich": ["FromLogContext", "WithMachineName", "WithThreadId", "WithEnvironmentName"]
}
```

### Request Logging
Cada request HTTP se registra con:
- Método HTTP
- Path
- Status code
- Tiempo de ejecución
- Host y esquema
- User Agent
- Correlation ID

### Ejemplo de Log
```
[2024-01-01 12:00:00.123 -05:00] [INF] [Chubb.Bot.AI.Assistant.Api] [abc-123-def] HTTP POST /api/faq/answer responded 200 in 125.4567 ms
```

## 7. CORS (Cross-Origin Resource Sharing)

### Características
- Configuración flexible de orígenes permitidos
- Soporte para credenciales
- Permite todos los métodos y headers

### Configuración
```json
"Cors": {
  "AllowedOrigins": [
    "http://localhost:3000",
    "http://localhost:4200"
  ]
}
```

### Política Aplicada
- **Origins**: Configurables desde appsettings.json
- **Methods**: Todos permitidos
- **Headers**: Todos permitidos
- **Credentials**: Permitidas

## Middleware Pipeline

El orden de ejecución de los middleware es:

1. **Serilog Request Logging** - Captura de información de requests
2. **Exception Handling** - Manejo centralizado de errores
3. **Correlation ID** - Tracking de requests
4. **Rate Limiting** - Control de tráfico
5. **CORS** - Control de acceso cross-origin
6. **Session** - Gestión de sesiones
7. **Authentication** - Autenticación JWT
8. **Authorization** - Autorización de endpoints

## Consideraciones de Producción

### Seguridad
- Cambiar la `SecretKey` de JWT en producción
- Configurar conexión segura a Redis (TLS)
- Ajustar límites de rate limiting según carga esperada
- Usar HTTPS en producción

### Rendimiento
- Monitorear latencia de Redis
- Ajustar timeouts de Polly según SLAs
- Configurar retención de logs apropiada
- Revisar límites de rate limiting

### Observabilidad
- Integrar con sistemas de monitoreo (Elasticsearch, Splunk, etc.)
- Configurar alertas en health checks
- Monitorear métricas de Polly (circuit breaker, retries)
- Trackear Correlation IDs en sistemas distribuidos

## Ejemplos de Uso

### Crear una Sesión
```csharp
var session = await _sessionService.CreateSessionAsync(
    userId: "user123",
    metadata: new Dictionary<string, string>
    {
        { "device", "mobile" },
        { "version", "1.0" }
    }
);
```

### Validar una Sesión
```csharp
var isValid = await _sessionService.ValidateSessionAsync(sessionId);
if (!isValid)
{
    return Unauthorized();
}
```

### Extender Tiempo de Sesión
```csharp
await _sessionService.ExtendSessionAsync(sessionId, additionalMinutes: 30);
```

## Troubleshooting

### Redis no conecta
- Verificar que Redis esté corriendo: `redis-cli ping`
- Revisar connection string en appsettings.json
- Verificar logs de Serilog para errores de conexión

### Rate Limiting muy restrictivo
- Ajustar límites en appsettings.json
- En desarrollo, aumentar límites en appsettings.Development.json
- Verificar que el IP del desarrollador esté en whitelist

### Circuit Breaker abierto
- Revisar health del servicio externo
- Verificar logs para ver razón de fallos
- Ajustar thresholds si es necesario

### Logs no se generan
- Verificar configuración de Serilog en appsettings.json
- Verificar permisos de escritura en carpeta logs/
- Revisar nivel de log mínimo configurado
