# Microservicios Backend

Este directorio contiene los 3 microservicios backend que el BFF consume:

## Servicios Disponibles

### 1. Chubb.Bot.AI.Assistant.QuoteBot (Puerto 5266)
Servicio de generación de cotizaciones de seguros.

**Endpoints:**
- `POST /api/quote/generate` - Generar una nueva cotización
- `GET /api/quote/health` - Health check del servicio

**Request Example:**
```json
{
  "sessionId": "session-123",
  "message": "I need auto insurance",
  "context": {
    "userId": "user-456"
  }
}
```

**Response Example:**
```json
{
  "quoteId": "quote-789",
  "sessionId": "session-123",
  "message": "Generated quote based on your request: I need auto insurance",
  "estimatedPrice": 2500,
  "currency": "USD",
  "items": [
    {
      "productName": "Auto Insurance",
      "coverage": "Comprehensive",
      "price": 800,
      "description": "Full coverage for your vehicle"
    }
  ],
  "generatedAt": "2026-01-29T10:00:00Z"
}
```

---

### 2. Chubb.Bot.AI.Assistant.FAQBot (Puerto 5267)
Servicio de preguntas frecuentes con búsqueda por keywords.

**Endpoints:**
- `POST /api/faq/query` - Consultar preguntas frecuentes
- `GET /api/faq/health` - Health check del servicio

**Request Example:**
```json
{
  "sessionId": "session-123",
  "question": "How do I file a claim?",
  "category": "Claims"
}
```

**Response Example:**
```json
{
  "responseId": "response-456",
  "sessionId": "session-123",
  "question": "How do I file a claim?",
  "answer": "To file a claim, log into your account and go to the Claims section, or call our 24/7 hotline.",
  "category": "Claims",
  "confidenceScore": 0.92,
  "relatedQuestions": [
    {
      "questionId": "q1",
      "questionText": "What types of insurance do you offer?",
      "category": "General"
    }
  ],
  "respondedAt": "2026-01-29T10:00:00Z"
}
```

**FAQ Database Keywords:**
- `coverage` - Information about insurance coverage
- `claim` - How to file claims
- `premium` - Premium calculation
- `cancel` - Policy cancellation
- `contact` - Contact information

---

### 3. Chubb.Bot.AI.Assistant.SpeechService (Puerto 7001)
Servicio de conversión de texto a voz y voz a texto.

**Endpoints:**
- `POST /api/speech/synthesize` - Convertir texto a audio (Text-to-Speech)
- `POST /api/speech/recognize` - Convertir audio a texto (Speech-to-Text)
- `GET /api/speech/health` - Health check del servicio

**Text-to-Speech Request:**
```json
{
  "text": "Welcome to Chubb Insurance",
  "language": "en-US",
  "voice": "female",
  "speed": 1.0,
  "format": "mp3"
}
```

**Text-to-Speech Response:**
```json
{
  "audioId": "audio-123",
  "audioUrl": "https://storage.example.com/audio/audio-123.mp3",
  "format": "mp3",
  "durationSeconds": 3,
  "fileSizeBytes": 24576,
  "generatedAt": "2026-01-29T10:00:00Z",
  "base64Audio": "U2ltdWxhdGVkIGF1ZGlvIGRhdGE="
}
```

**Speech-to-Text Request:**
```json
{
  "audioUrl": "https://example.com/audio.mp3",
  "language": "en-US",
  "format": "mp3"
}
```

**Speech-to-Text Response:**
```json
{
  "transcriptionId": "trans-789",
  "text": "I would like to get a quote for auto insurance",
  "confidence": 0.95,
  "language": "en-US",
  "durationSeconds": 5,
  "words": [
    {
      "text": "I",
      "startTime": 0.0,
      "endTime": 0.5,
      "confidence": 0.98
    }
  ],
  "processedAt": "2026-01-29T10:00:00Z"
}
```

---

## Compilar Todos los Servicios

```bash
# Compilar Chubb.Bot.AI.Assistant.QuoteBot
cd Chubb.Bot.AI.Assistant.QuoteBot
dotnet build

# Compilar Chubb.Bot.AI.Assistant.FAQBot
cd ../Chubb.Bot.AI.Assistant.FAQBot
dotnet build

# Compilar Chubb.Bot.AI.Assistant.SpeechService
cd ../Chubb.Bot.AI.Assistant.SpeechService
dotnet build
```

## Ejecutar los Servicios

### Ejecutar cada servicio por separado:

**Chubb.Bot.AI.Assistant.QuoteBot:**
```bash
cd Chubb.Bot.AI.Assistant.QuoteBot
dotnet run
```
Estará disponible en `http://localhost:5266`
Swagger UI: `http://localhost:5266/swagger`

**Chubb.Bot.AI.Assistant.FAQBot:**
```bash
cd Chubb.Bot.AI.Assistant.FAQBot
dotnet run
```
Estará disponible en `http://localhost:5267`
Swagger UI: `http://localhost:5267/swagger`

**Chubb.Bot.AI.Assistant.SpeechService:**
```bash
cd Chubb.Bot.AI.Assistant.SpeechService
dotnet run
```
Estará disponible en `http://localhost:7001`
Swagger UI: `http://localhost:7001/swagger`

## Ejecutar todos los servicios simultáneamente

Puedes abrir 3 terminales diferentes y ejecutar cada servicio en una terminal separada.

### En Windows (PowerShell):
```powershell
# Terminal 1
cd microservicios/Chubb.Bot.AI.Assistant.QuoteBot
dotnet run

# Terminal 2
cd microservicios/Chubb.Bot.AI.Assistant.FAQBot
dotnet run

# Terminal 3
cd microservicios/Chubb.Bot.AI.Assistant.SpeechService
dotnet run
```

## Health Checks

Todos los servicios tienen un endpoint de health check:

```bash
# Chubb.Bot.AI.Assistant.QuoteBot
curl http://localhost:5266/health

# Chubb.Bot.AI.Assistant.FAQBot
curl http://localhost:5267/health

# Chubb.Bot.AI.Assistant.SpeechService
curl http://localhost:7001/health
```

## Integración con el BFF

El BFF está configurado para conectarse automáticamente a estos servicios en los puertos especificados. Asegúrate de que todos los servicios estén corriendo antes de iniciar el BFF.

La configuración del BFF se encuentra en `BFF.Api/appsettings.json`:

```json
{
  "HttpClients": {
    "Chubb.Bot.AI.Assistant.QuoteBot": {
      "BaseUrl": "http://localhost:5266",
      "TimeoutSeconds": 10,
      "RetryCount": 3
    },
    "Chubb.Bot.AI.Assistant.FAQBot": {
      "BaseUrl": "http://localhost:5267",
      "TimeoutSeconds": 10,
      "RetryCount": 3
    },
    "Chubb.Bot.AI.Assistant.SpeechService": {
      "BaseUrl": "http://localhost:7001",
      "TimeoutSeconds": 30,
      "RetryCount": 2
    }
  }
}
```

## Notas

- Todos los servicios son simulados y devuelven datos de prueba
- Los servicios no requieren autenticación (para simplificar el desarrollo)
- Los servicios no persisten datos (todo en memoria)
- El BFF maneja la resiliencia con Polly (Retry, Circuit Breaker, Timeout)
- Cada servicio tiene Swagger UI para testing manual

## Troubleshooting

### Error: "The configured port is already in use"

Verifica que no haya otros procesos usando los puertos 5266, 5267 o 7001:

```bash
# Windows
netstat -ano | findstr :5266
netstat -ano | findstr :5267
netstat -ano | findstr :7001
```

### Error: "Unable to bind to http://localhost:XXXX"

Asegúrate de que tienes permisos para abrir puertos en tu sistema.

## Próximos Pasos

- Implementar persistencia de datos (base de datos)
- Agregar autenticación JWT
- Implementar logging estructurado
- Agregar más casos de prueba en los controllers
- Implementar rate limiting
- Agregar métricas y monitoring
