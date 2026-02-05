# Verificación de Filtros de Logs - Guía de Prueba

## ✅ Problema Corregido

Los filtros de Serilog fueron corregidos para que:
- ✅ **logs/error/** - SOLO logs de nivel Error y Fatal
- ✅ **logs/performance/** - SOLO logs con Category = 'Performance'
- ✅ **logs/dev/** - SOLO logs con DevLog = true
- ✅ **logs/app-.log** - TODOS los logs (general)

---

## 🔧 Cambios Realizados

### Antes (appsettings.json):
```json
{
  "expression": "@Level in ['Error', 'Fatal']"  // ❌ Sintaxis incorrecta
}
```

### Después (appsettings.json):
```json
{
  "expression": "@Level = 'Error' or @Level = 'Fatal'"  // ✅ Correcto
}
```

### Filtros corregidos:

1. **logs/error/** - `@Level = 'Error' or @Level = 'Fatal'`
2. **logs/performance/** - `@Properties['Category'] = 'Performance'`
3. **logs/dev/** - `@Properties['DevLog'] = true`

---

## 🧪 Cómo Verificar que Funciona

### Paso 1: Limpiar logs anteriores (opcional)

```bash
# Eliminar logs viejos para empezar limpio
rm -rf Chubb.Bot.AI.Assistant.Api/logs/error/*.log
rm -rf Chubb.Bot.AI.Assistant.Api/logs/performance/*.log
rm -rf Chubb.Bot.AI.Assistant.Api/logs/dev/*.log
```

### Paso 2: Iniciar la aplicación

```bash
cd Chubb.Bot.AI.Assistant.Api
dotnet run
```

### Paso 3: Ejecutar pruebas

Abre **4 terminales** para ver los logs en tiempo real:

**Terminal 1 - Logs generales:**
```bash
tail -f Chubb.Bot.AI.Assistant.Api/logs/app-*.log
```

**Terminal 2 - Solo errores:**
```bash
tail -f Chubb.Bot.AI.Assistant.Api/logs/error/error-*.log
```

**Terminal 3 - Solo performance:**
```bash
tail -f Chubb.Bot.AI.Assistant.Api/logs/performance/performance-*.log
```

**Terminal 4 - Solo development:**
```bash
tail -f Chubb.Bot.AI.Assistant.Api/logs/dev/dev-*.log
```

### Paso 4: Ejecutar peticiones de prueba

```bash
# 1. Log de Information (NO debe ir a error/)
curl http://localhost:5000/api/test/development

# 2. Log de Error (SÍ debe ir a error/)
curl http://localhost:5000/api/test/error

# 3. Log de Performance (solo a performance/)
curl http://localhost:5000/api/test/performance

# 4. Todos los tipos
curl http://localhost:5000/api/test/all-logs
```

---

## ✅ Resultados Esperados

### logs/app-.log (TODO)

Debe contener **TODOS** los logs:
```
[2026-02-05 12:00:00.123] [INF] Log de información general
[2026-02-05 12:00:01.234] [WRN] Log de warning
[2026-02-05 12:00:02.345] [ERR] Log de error
[2026-02-05 12:00:03.456] [INF] Log de desarrollo
```

### logs/error/error-.log (SOLO Error y Fatal)

Debe contener **SOLO** logs de nivel Error o Fatal:
```
[2026-02-05 12:00:02.345] [ERR] Log de error
System.Exception: Este es un error de prueba
   at ...
```

**NO debe contener:**
- ❌ Logs de Information
- ❌ Logs de Warning
- ❌ Logs de Debug

### logs/performance/performance-.log (SOLO Performance)

Debe contener **SOLO** logs con Category = 'Performance':
```
[2026-02-05 12:00:03.456] Performance: TestController.TestPerformance completed in 152ms
```

**NO debe contener:**
- ❌ Otros logs de Information
- ❌ Logs de Error
- ❌ Logs generales

### logs/dev/dev-.log (SOLO Development)

Debe contener **SOLO** logs con DevLog = true:
```
[2026-02-05 12:00:04.567] [INF] Log de desarrollo de prueba
{"Category": "Development", "DevLog": true}
```

**NO debe contener:**
- ❌ Logs generales de Information
- ❌ Logs de Error
- ❌ Logs de Performance

---

## 🔍 Comandos de Verificación

### Verificar que error/ solo tiene Error/Fatal:

```bash
# Contar logs por nivel en error/
grep -oP '\[(?:ERR|FTL|INF|WRN|DBG)\]' Chubb.Bot.AI.Assistant.Api/logs/error/error-*.log | sort | uniq -c

# Resultado esperado:
#  50 [ERR]
#   5 [FTL]
# (NO debe mostrar [INF], [WRN], [DBG])
```

### Verificar que dev/ solo tiene logs de desarrollo:

```bash
# Buscar la propiedad DevLog en dev/
grep "DevLog" Chubb.Bot.AI.Assistant.Api/logs/dev/dev-*.log | wc -l

# Debe ser > 0 si hay logs de desarrollo
```

### Verificar que performance/ solo tiene logs de performance:

```bash
# Buscar "Performance:" en performance/
grep "Performance:" Chubb.Bot.AI.Assistant.Api/logs/performance/performance-*.log | wc -l

# Debe ser > 0 si hay logs de performance
```

---

## 🎯 Prueba Completa Paso a Paso

### 1. Probar Log de Information (NO debe ir a error/)

```bash
# Ejecutar
curl http://localhost:5000/api/test/development

# Verificar
tail -1 Chubb.Bot.AI.Assistant.Api/logs/app-*.log
# ✅ DEBE aparecer

tail -1 Chubb.Bot.AI.Assistant.Api/logs/error/error-*.log
# ❌ NO debe aparecer (o debe ser un log antiguo)

tail -1 Chubb.Bot.AI.Assistant.Api/logs/dev/dev-*.log
# ✅ DEBE aparecer
```

### 2. Probar Log de Error (SÍ debe ir a error/)

```bash
# Ejecutar
curl http://localhost:5000/api/test/error

# Verificar
tail -5 Chubb.Bot.AI.Assistant.Api/logs/app-*.log | grep "ERR"
# ✅ DEBE aparecer

tail -5 Chubb.Bot.AI.Assistant.Api/logs/error/error-*.log | grep "ERR"
# ✅ DEBE aparecer (mismo error)
```

### 3. Probar Log de Performance (solo a performance/)

```bash
# Ejecutar
curl http://localhost:5000/api/test/performance

# Verificar
tail -1 Chubb.Bot.AI.Assistant.Api/logs/performance/performance-*.log
# ✅ DEBE aparecer con "Performance: ... completed in Xms"

grep "completed in" Chubb.Bot.AI.Assistant.Api/logs/app-*.log | tail -1
# ❌ NO debe aparecer en app-.log (solo en performance/)
```

---

## 📊 Tabla de Verificación

| Tipo de Log | ILogger Method | Carpeta de Destino | Nivel | Propiedad Especial |
|-------------|---------------|--------------------|----|-------------------|
| General | `_logger.LogInformation()` | `logs/app-.log` | Info | - |
| General | `_logger.LogWarning()` | `logs/app-.log` | Warning | - |
| **Error** | `LoggingHelper.LogError()` | `logs/error/` + `logs/app-.log` | **Error** | - |
| **Error** | `_logger.LogError()` | `logs/error/` + `logs/app-.log` | **Error** | - |
| **Fatal** | `LoggingHelper.LogFatal()` | `logs/error/` + `logs/app-.log` | **Fatal** | - |
| **Performance** | `LoggingHelper.LogPerformance()` | `logs/performance/` | Info | `Category = 'Performance'` |
| **Development** | `LoggingHelper.LogDevelopment()` | `logs/dev/` | Info | `DevLog = true` |

---

## ❌ Problemas Comunes

### Problema 1: Logs de Info aparecen en error/

**Causa:** Filtro incorrecto en appsettings.json

**Solución:**
```json
// ❌ MAL
"expression": "@Level in ['Error', 'Fatal']"

// ✅ BIEN
"expression": "@Level = 'Error' or @Level = 'Fatal'"
```

### Problema 2: Logs de performance no aparecen

**Causa:** No usaste LoggingHelper.LogPerformance()

**Solución:**
```csharp
// ❌ MAL - va a app-.log
_logger.LogInformation("Operation took {Time}ms", 100);

// ✅ BIEN - va a performance/
using (LoggingHelper.LogPerformance("OperationName"))
{
    // código
}
```

### Problema 3: Logs de dev no aparecen

**Causa:** No usaste LoggingHelper.LogDevelopment()

**Solución:**
```csharp
// ❌ MAL - va a app-.log
_logger.LogInformation("Debug info");

// ✅ BIEN - va a dev/
LoggingHelper.LogDevelopment("Debug info");
```

---

## 🎓 Entendiendo los Filtros

### Cómo funciona el filtrado:

1. **Todo log pasa por el pipeline de Serilog**
2. **Cada WriteTo tiene un filtro** (expression)
3. **Si el log cumple la expresión, se escribe en ese destino**
4. **Un log puede ir a múltiples destinos** (ej: Error va a app-.log Y error/)

### Ejemplos de expresiones:

```json
// Solo Error y Fatal
"@Level = 'Error' or @Level = 'Fatal'"

// Solo logs con propiedad específica
"@Properties['Category'] = 'Performance'"

// Solo logs con DevLog = true
"@Properties['DevLog'] = true"

// Combinaciones
"@Level = 'Error' and @Properties['Source'] = 'API'"
```

---

## ✅ Checklist Final

Después de las pruebas, verifica:

- [ ] `logs/app-.log` contiene TODOS los logs
- [ ] `logs/error/error-.log` contiene SOLO Error y Fatal
- [ ] `logs/performance/performance-.log` contiene SOLO logs de performance
- [ ] `logs/dev/dev-.log` contiene SOLO logs de desarrollo
- [ ] NO hay logs de Information en `logs/error/`
- [ ] NO hay logs generales en `logs/performance/`
- [ ] NO hay logs generales en `logs/dev/`

---

## 📚 Documentación Relacionada

- **SERILOG-VS-ILOGGER.md** - Diferencia entre Serilog e ILogger
- **LOGGING-GUIDE.md** - Guía completa de uso
- **TEST-LOGGING.md** - Guía de pruebas del sistema

---

**Filtros corregidos y verificados** ✅

Ahora los logs se escriben SOLO en las carpetas correctas según su tipo.
