# Solución de Sistema de Logging - Resumen

## Problema Identificado

El sistema de logging estaba configurado con Serilog pero **solo escribía en un archivo general** (`logs/app-.log`). No existían carpetas separadas para errores, performance ni desarrollo.

## Solución Implementada

### 1. Configuración de Carpetas Separadas

Se configuraron **4 carpetas de logs** con propósitos específicos:

```
logs/
├── app-YYYYMMDD.log              # Logs generales (30 días de retención)
├── error/
│   └── error-YYYYMMDD.log        # Solo errores Error y Fatal (90 días)
├── performance/
│   └── performance-YYYYMMDD.log  # Métricas de rendimiento (30 días)
└── dev/
    └── dev-YYYYMMDD.log          # Logs de desarrollo (7 días)
```

### 2. Archivos Modificados/Creados

#### Archivos Modificados:
1. **`appsettings.json`** - Configuración de Serilog con filtros para cada carpeta
2. **`Program.cs`** - Inicialización de carpetas y configuración mejorada

#### Archivos Creados:
3. **`Helpers/LoggingHelper.cs`** - Clase helper con métodos convenientes
4. **`LOGGING-GUIDE.md`** - Documentación completa de uso
5. **`Controllers/ChatController.cs`** - Actualizado con ejemplos de uso

#### Paquetes Instalados:
- **`Serilog.Expressions`** - Para filtros avanzados de logs

### 3. Características Implementadas

#### ✅ Logs de Error Automáticos
```csharp
LoggingHelper.LogError("Error message", exception, params);
LoggingHelper.LogFatal("Critical error", exception);
```
- Se escriben automáticamente en `logs/error/`
- Retención de 90 días
- Incluyen stack traces completos

#### ✅ Logs de Performance
```csharp
using (LoggingHelper.LogPerformance("OperationName"))
{
    // Tu código aquí
    // Se mide automáticamente el tiempo
}
```
- Se escriben en `logs/performance/`
- Miden tiempo de ejecución automáticamente
- Permiten agregar contexto adicional

#### ✅ Logs de Desarrollo
```csharp
LoggingHelper.LogDevelopment("Debug message", params);
LoggingHelper.LogDevelopmentObject("Object info", object);
```
- Se escriben en `logs/dev/`
- Ideales para debugging
- Retención de solo 7 días

### 4. Inicialización Automática

Las carpetas de logs se **crean automáticamente** al iniciar la aplicación:
```csharp
LoggingHelper.InitializeLogDirectories();
```

### 5. Ejemplo de Uso Completo

Ver `ChatController.cs` para un ejemplo completo que incluye:
- ✅ Performance logging (tiempo de ejecución)
- ✅ Error logging (con excepciones)
- ✅ Development logging (debugging)
- ✅ Contexto adicional en logs

## Cómo Usar

### Para Logs de Error:
```csharp
using Chubb.Bot.AI.Assistant.Api.Helpers;

try
{
    // código
}
catch (Exception ex)
{
    LoggingHelper.LogError("Error description", ex);
}
```

### Para Logs de Performance:
```csharp
using (LoggingHelper.LogPerformance("MyOperation"))
{
    // código a medir
}
```

### Para Logs de Desarrollo:
```csharp
LoggingHelper.LogDevelopment("Debug info: {Value}", value);
LoggingHelper.LogDevelopmentObject("Request received", requestObject);
```

## Verificación

Para verificar que funciona:

1. **Inicia la aplicación**
2. **Verifica que se crearon las carpetas**:
   ```bash
   ls -la Chubb.Bot.AI.Assistant.Api/logs/
   ```
   Deberías ver: `error/`, `performance/`, `dev/`

3. **Realiza una petición al API** (ej: POST a `/api/chat`)

4. **Verifica los logs**:
   ```bash
   # Logs generales
   tail -f Chubb.Bot.AI.Assistant.Api/logs/app-*.log

   # Logs de performance (deberías ver el tiempo de la operación)
   tail -f Chubb.Bot.AI.Assistant.Api/logs/performance/performance-*.log

   # Logs de desarrollo
   tail -f Chubb.Bot.AI.Assistant.Api/logs/dev/dev-*.log
   ```

5. **Causa un error** (ej: servicio externo no disponible)

6. **Verifica logs de error**:
   ```bash
   tail -f Chubb.Bot.AI.Assistant.Api/logs/error/error-*.log
   ```

## Monitoreo en Producción

### Ver logs en tiempo real:
```bash
# Todos los errores del día
tail -f logs/error/error-$(date +%Y%m%d).log

# Operaciones lentas (>1000ms)
grep -E "completed in [0-9]{4,}ms" logs/performance/performance-*.log

# Buscar por session ID
grep "SessionId: 12345" logs/app-*.log
```

### Analizar performance:
```bash
# Top 10 operaciones más lentas
grep "completed in" logs/performance/performance-*.log | sort -t':' -k4 -rn | head -10
```

## Rotación y Retención

Todos los logs rotan **diariamente** y se retienen según:
- **General**: 30 días
- **Error**: 90 días (para análisis histórico)
- **Performance**: 30 días
- **Development**: 7 días (solo para debugging reciente)

## Migración de Código Existente

Para migrar código existente:

### Antes:
```csharp
try
{
    var result = await service.GetData();
    return Ok(result);
}
catch (Exception ex)
{
    _logger.LogError(ex, "Error getting data");
    return StatusCode(500);
}
```

### Después:
```csharp
using (var perfLogger = LoggingHelper.LogPerformance("GetData"))
{
    try
    {
        LoggingHelper.LogDevelopment("Fetching data from service");

        var result = await service.GetData();

        perfLogger.AddContext("RecordsReturned", result.Count);

        return Ok(result);
    }
    catch (Exception ex)
    {
        LoggingHelper.LogError("Error getting data from service", ex);
        return StatusCode(500);
    }
}
```

## Próximos Pasos (Opcional)

1. **Application Insights**: Integrar con Azure para monitoreo en la nube
2. **Elk Stack**: Centralizar logs de múltiples servicios
3. **Alertas**: Configurar alertas para errores críticos
4. **Dashboards**: Crear dashboards de métricas de performance

## Documentación Completa

Ver **`LOGGING-GUIDE.md`** para:
- Ejemplos detallados de cada tipo de log
- Best practices
- Guía de troubleshooting
- Integración con herramientas externas

---

**Problema resuelto**: Los logs ahora se escriben correctamente en carpetas separadas para error, performance y desarrollo.

**Implementación completada**: 2026-02-05
