# Quick Start - Sistema de Logging

## ✅ Solución Implementada y Corregida

El sistema de logging ahora escribe en **4 carpetas separadas** con **filtros correctos**:

```
logs/
├── app-YYYYMMDD.log              # TODOS los logs (general)
├── error/error-YYYYMMDD.log      # SOLO Error y Fatal
├── performance/performance-YYYYMMDD.log  # SOLO logs de performance
└── dev/dev-YYYYMMDD.log          # SOLO logs de desarrollo
```

### 🔧 Filtros Corregidos:
- ✅ **logs/error/** - Solo logs con `@Level = 'Error' or @Level = 'Fatal'`
- ✅ **logs/performance/** - Solo logs con `@Properties['Category'] = 'Performance'`
- ✅ **logs/dev/** - Solo logs con `@Properties['DevLog'] = true`
- ✅ **NO** hay logs de Information en logs/error/

## 🚀 Cómo Verificar que Funciona

### 1. Inicia la aplicación
```bash
cd Chubb.Bot.AI.Assistant.Api
dotnet run
```

### 2. Las carpetas se crean automáticamente
Al iniciar, verás en la consola:
```
[INF] Created log directory: logs
[INF] Created log directory: logs/error
[INF] Created log directory: logs/performance
[INF] Created log directory: logs/dev
```

### 3. Verifica las carpetas
```bash
ls -la Chubb.Bot.AI.Assistant.Api/logs/
```
Deberías ver: `error/`, `performance/`, `dev/`

### 4. Haz una petición al API
```bash
curl -X POST http://localhost:5000/api/chat \
  -H "Content-Type: application/json" \
  -d '{"message":"test","sessionId":"123","botId":"1"}'
```

### 5. Verifica los logs generados

#### Logs de Performance
```bash
tail -f Chubb.Bot.AI.Assistant.Api/logs/performance/performance-*.log
```
Deberías ver:
```
Performance: ChatController.SendMessage completed in 145ms
```

#### Logs de Desarrollo
```bash
tail -f Chubb.Bot.AI.Assistant.Api/logs/dev/dev-*.log
```
Deberías ver:
```
Processing chat message for session 123 with bot 1
```

#### Logs de Error (si hay errores)
```bash
tail -f Chubb.Bot.AI.Assistant.Api/logs/error/error-*.log
```

## 📝 Cómo Usar en Tu Código

### Opción 1: ILogger (Logs generales - Recomendado)

```csharp
// Inyectar ILogger en el constructor
private readonly ILogger<MyController> _logger;

public MyController(ILogger<MyController> logger)
{
    _logger = logger;
}

// Usar en tu código
_logger.LogInformation("User {UserId} logged in", userId);  // → logs/app-.log
_logger.LogWarning("Cache expired");                         // → logs/app-.log
_logger.LogError(exception, "Error processing");             // → logs/app-.log Y logs/error/
```

**✅ Usa ILogger para la mayoría de logs**

### Opción 2: LoggingHelper (Casos especiales)

```csharp
using Chubb.Bot.AI.Assistant.Api.Helpers;

// 1. Error que DEBE ir a logs/error/
try {
    // código
}
catch (Exception ex) {
    LoggingHelper.LogError("Error description", ex);  // → logs/error/ Y logs/app-.log
}

// 2. Medir performance
using (LoggingHelper.LogPerformance("OperationName"))
{
    // código a medir  // → logs/performance/ (solo)
}

// 3. Logs de desarrollo
LoggingHelper.LogDevelopment("Debug info: {Value}", value);  // → logs/dev/ (solo)
```

**✅ Usa LoggingHelper solo para casos especiales**

## 📚 Documentación Completa

Ver los siguientes archivos para más detalles:

1. **`VERIFICAR-FILTROS-LOGS.md`** - **IMPORTANTE** - Cómo verificar que los filtros funcionan
2. **`SERILOG-VS-ILOGGER.md`** - Diferencia entre Serilog e ILogger (cuándo usar cada uno)
3. **`LOGGING-SOLUTION.md`** - Resumen ejecutivo de la solución
4. **`LOGGING-GUIDE.md`** - Guía completa con ejemplos y best practices
5. **`TEST-LOGGING.md`** - Guía de pruebas del sistema

## 🔧 Archivos Modificados

- ✅ `appsettings.json` - Configuración de Serilog con filtros
- ✅ `Program.cs` - Inicialización de carpetas
- ✅ `Helpers/LoggingHelper.cs` - Helper class (NUEVO)
- ✅ `Controllers/ChatController.cs` - Ejemplo de uso

## 📦 Paquetes Instalados

- ✅ `Serilog.Expressions` v5.0.0

## ✨ Características

✅ **Filtros corregidos** - Solo errores en logs/error/, solo performance en logs/performance/
✅ Logs de error automáticos en `logs/error/` (SOLO Error y Fatal)
✅ Logs de performance con medición de tiempo en `logs/performance/` (SOLO performance)
✅ Logs de desarrollo para debugging en `logs/dev/` (SOLO development)
✅ Carpetas creadas automáticamente al iniciar
✅ Rotación diaria de archivos
✅ Retención configurable (30/90/7 días)
✅ ILogger es suficiente para la mayoría de casos
✅ Compilación exitosa verificada

## 🎯 Próximos Pasos

1. Inicia la aplicación
2. Verifica que se crean las carpetas
3. Realiza peticiones y verifica los logs
4. Implementa el logging en otros controllers siguiendo el ejemplo de `ChatController.cs`

---

**Todo listo para usar!** 🎉
