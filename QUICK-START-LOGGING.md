# Quick Start - Sistema de Logging

## ✅ Solución Implementada

El sistema de logging ahora escribe en **4 carpetas separadas**:

```
logs/
├── app-YYYYMMDD.log              # Logs generales
├── error/error-YYYYMMDD.log      # Solo errores (Error y Fatal)
├── performance/performance-YYYYMMDD.log  # Métricas de rendimiento
└── dev/dev-YYYYMMDD.log          # Logs de desarrollo
```

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

### 1. Agregar el using
```csharp
using Chubb.Bot.AI.Assistant.Api.Helpers;
```

### 2. Log de Error
```csharp
try {
    // tu código
}
catch (Exception ex) {
    LoggingHelper.LogError("Error description", ex);
}
```

### 3. Log de Performance
```csharp
using (LoggingHelper.LogPerformance("OperationName"))
{
    // código a medir
}
```

### 4. Log de Desarrollo
```csharp
LoggingHelper.LogDevelopment("Debug info: {Value}", value);
```

## 📚 Documentación Completa

Ver los siguientes archivos para más detalles:

1. **`LOGGING-SOLUTION.md`** - Resumen ejecutivo de la solución
2. **`LOGGING-GUIDE.md`** - Guía completa con ejemplos y best practices

## 🔧 Archivos Modificados

- ✅ `appsettings.json` - Configuración de Serilog con filtros
- ✅ `Program.cs` - Inicialización de carpetas
- ✅ `Helpers/LoggingHelper.cs` - Helper class (NUEVO)
- ✅ `Controllers/ChatController.cs` - Ejemplo de uso

## 📦 Paquetes Instalados

- ✅ `Serilog.Expressions` v5.0.0

## ✨ Características

✅ Logs de error automáticos en `logs/error/`
✅ Logs de performance con medición de tiempo en `logs/performance/`
✅ Logs de desarrollo para debugging en `logs/dev/`
✅ Carpetas creadas automáticamente al iniciar
✅ Rotación diaria de archivos
✅ Retención configurable (30/90/7 días)
✅ Compilación exitosa verificada

## 🎯 Próximos Pasos

1. Inicia la aplicación
2. Verifica que se crean las carpetas
3. Realiza peticiones y verifica los logs
4. Implementa el logging en otros controllers siguiendo el ejemplo de `ChatController.cs`

---

**Todo listo para usar!** 🎉
