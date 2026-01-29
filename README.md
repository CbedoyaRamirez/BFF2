# BFF - Backend For Frontend API

Backend For Frontend (BFF) completo desarrollado en .NET 8.0 que actúa como capa de orquestación entre un frontend Angular 20 y tres servicios backend (QuoteBot, FAQBot y SpeechService).

## Arquitectura

El proyecto sigue una arquitectura limpia (Clean Architecture) con separación de responsabilidades en 4 capas:

```
BFF2/
├── Chubb.Bot.AI.Assistant.Api/                    # Web API Layer - Controllers, Middleware
├── Chubb.Bot.AI.Assistant.Application/            # Business Logic - Services, DTOs, Validators
├── Chubb.Bot.AI.Assistant.Infrastructure/         # External Dependencies - HTTP, Redis, Polly
├── Chubb.Bot.AI.Assistant.Core/                   # Domain Models - Entities, Enums, Exceptions
├── Chubb.Bot.AI.Assistant.Tests.Unit/            # Unit Tests
└── Chubb.Bot.AI.Assistant.Tests.Integration/     # Integration Tests
```

## Componentes Principales

### 1. Controllers
- **SessionController**: Gestión de sesiones de usuario
- **HealthController**: Health checks y diagnóstico

### 2. Middleware Pipeline
1. ExceptionHandlingMiddleware (captura todas las excepciones)
2. CorrelationIdMiddleware (genera/propaga X-Correlation-ID)
3. CORS
4. Authentication (JWT Bearer)
5. Authorization
6. Rate Limiting

### 3. Application Services
- **SessionService**: Gestión de sesiones en Redis con TTL configurable
- **ConversationOrchestrator**: Orquesta llamadas a servicios externos (pendiente)
- **ResponseAggregator**: Combina y enriquece respuestas (pendiente)
- **CacheService**: Wrapper genérico sobre IDistributedCache (pendiente)

### 4. HTTP Clients con Polly Resilience
- **QuoteBotClient**: Comunicación con Quote Bot Service (:5266)
- **FAQBotClient**: Comunicación con FAQ Bot Service (:5267)
- **SpeechClient**: Comunicación con Speech Service (:7001)

Cada cliente implementa:
- Retry Policy (3 intentos con exponential backoff)
- Circuit Breaker (5 fallos → abierto 30s)
- Timeout Policy (configurable por servicio)
- Correlation ID propagation
- Request/Response logging

### 5. Infraestructura
- **Redis**: Almacenamiento de sesiones y cache (localhost:6379)
- **Health Checks**: Monitoreo de Redis y servicios externos
- **Serilog**: Logging estructurado con correlation IDs (configurable)

## Tecnologías y Paquetes

- **.NET 8.0**: Framework principal
- **JWT Bearer Authentication**: Autenticación stateless
- **Redis (StackExchange.Redis)**: Cache y gestión de sesiones
- **Polly**: Resiliencia y políticas de retry/circuit breaker
- **FluentValidation**: Validación declarativa de DTOs
- **Swagger/OpenAPI**: Documentación de API
- **xUnit, Moq, FluentAssertions**: Testing

## Prerrequisitos

- .NET 8.0 SDK o superior
- Docker (para Redis)
- (Opcional) Servicios externos QuoteBot, FAQBot, SpeechService

## Configuración

### 1. Iniciar Redis

```bash
docker-compose up -d
```

Esto iniciará Redis en `localhost:6379`.

### 2. Configurar appsettings.json

El archivo `Chubb.Bot.AI.Assistant.Api/appsettings.json` contiene toda la configuración:

```json
{
  "JwtSettings": {
    "SecretKey": "your-secret-key-minimum-32-characters-long-change-in-production",
    "Issuer": "Chubb.Bot.AI.Assistant.Api",
    "Audience": "BFF.Client",
    "ExpirationMinutes": 60
  },
  "RedisSettings": {
    "ConnectionString": "localhost:6379",
    "InstanceName": "BFF:",
    "DefaultTTLMinutes": 30
  },
  "HttpClients": {
    "QuoteBot": {
      "BaseUrl": "http://localhost:5266",
      "TimeoutSeconds": 10,
      "RetryCount": 3
    },
    "FAQBot": {
      "BaseUrl": "http://localhost:5267",
      "TimeoutSeconds": 10,
      "RetryCount": 3
    },
    "SpeechService": {
      "BaseUrl": "http://localhost:7001",
      "TimeoutSeconds": 30,
      "RetryCount": 2
    }
  },
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:3000",
      "http://localhost:4200"
    ]
  }
}
```

## Ejecución

### Compilar el proyecto

```bash
dotnet build Chubb.Bot.AI.Assistant.sln
```

### Ejecutar la API

```bash
cd Chubb.Bot.AI.Assistant.Api
dotnet run
```

La API estará disponible en:
- HTTPS: `https://localhost:5001`
- HTTP: `http://localhost:5000`
- Swagger UI: `https://localhost:5001/swagger`

### Ejecutar Tests

```bash
dotnet test Chubb.Bot.AI.Assistant.sln
```

## Endpoints Principales

### Session Management

**POST /api/sessions**
Crear nueva sesión

```bash
curl -X POST https://localhost:5001/api/sessions \
  -H "Content-Type: application/json" \
  -d '{"userId": "user123"}'
```

**GET /api/sessions/{sessionId}**
Obtener información de sesión

**DELETE /api/sessions/{sessionId}**
Eliminar sesión

### Health Checks

**GET /health**
Verificar estado del sistema y dependencias

```bash
curl https://localhost:5001/health
```

Respuesta:
```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.123",
  "entries": {
    "redis": { "status": "Healthy" },
    "quotebot": { "status": "Degraded" },
    "faqbot": { "status": "Degraded" },
    "speechservice": { "status": "Degraded" }
  }
}
```

## Desarrollo y Extensión

### Agregar un nuevo Controller

1. Crear el controller en `Chubb.Bot.AI.Assistant.Api/Controllers/`
2. Inyectar servicios necesarios
3. Los endpoints se registran automáticamente con `MapControllers()`

### Agregar un nuevo Service

1. Definir interfaz en `Chubb.Bot.AI.Assistant.Application/Interfaces/`
2. Implementar en `Chubb.Bot.AI.Assistant.Application/Services/`
3. Registrar en `Program.cs`: `builder.Services.AddScoped<IService, Service>()`

### Agregar un nuevo HTTP Client

1. Definir interfaz en `Chubb.Bot.AI.Assistant.Infrastructure/HttpClients/Interfaces/`
2. Implementar en `Chubb.Bot.AI.Assistant.Infrastructure/HttpClients/`
3. Configurar en `Program.cs` con IHttpClientFactory y Polly
4. Agregar configuración en `appsettings.json`

## Troubleshooting

### Error: Redis no está disponible
Verificar que Docker esté corriendo:
```bash
docker ps | grep bff-redis
```

Si no está corriendo:
```bash
docker-compose up -d
```

### Error: No puedo autenticar con JWT
Asegúrate de que el `SecretKey` en `appsettings.json` tiene al menos 32 caracteres.

### Servicios externos no disponibles
Los health checks mostrarán "Degraded" si los servicios externos no están disponibles. Esto es normal en desarrollo y no afecta el funcionamiento de las sesiones y health checks.

## Próximos Pasos (Pendientes)

- Implementar ConversationOrchestrator completo
- Implementar ResponseAggregator
- Implementar CacheService genérico
- Agregar ChatController con lógica de orquestación
- Agregar SpeechController
- Implementar Rate Limiting activo
- Agregar más tests unitarios y de integración
- Configurar Serilog completo con enrichers
- Implementar AutoMapper profiles

## Notas de Seguridad

- **JWT SecretKey**: Cambiar el secreto en producción
- **CORS**: Configurar orígenes específicos en producción
- **Redis**: Usar conexión segura en producción
- **Secrets**: Usar User Secrets en desarrollo, Azure Key Vault en producción

## Licencia

Proyecto desarrollado para Chubb.

## Autor

Generado con Claude Code - Anthropic
