# Guía de Pruebas - Sistema de Logging

## ✅ Problema Resuelto: ExceptionHandlingMiddleware

El middleware de manejo de excepciones ahora **escribe correctamente en la carpeta logs/error/** usando `LoggingHelper`.

### Cambios Realizados

1. ✅ Agregado `using Chubb.Bot.AI.Assistant.Api.Helpers`
2. ✅ Errores no manejados ahora usan `LoggingHelper.LogError()` para escribir en `logs/error/`
3. ✅ Logs de desarrollo agregados para debugging en `logs/dev/`
4. ✅ Warnings y errores de negocio se mantienen como warnings (no van a `logs/error/`)

## 🚀 Cómo Probar el Sistema

### Paso 1: Iniciar la Aplicación

```bash
cd Chubb.Bot.AI.Assistant.Api
dotnet run
```

### Paso 2: Verificar que se crean las carpetas

Deberías ver en la consola:
```
[INF] Created log directory: logs
[INF] Created log directory: logs/error
[INF] Created log directory: logs/performance
[INF] Created log directory: logs/dev
```

### Paso 3: Endpoints de Prueba Disponibles

Se creó un **TestController** con endpoints para probar cada tipo de log:

#### 1. Información de Pruebas
```bash
GET http://localhost:5000/api/test/info
```
Muestra todos los endpoints disponibles y cómo usarlos.

#### 2. Probar Logs de Error (ExceptionHandlingMiddleware)
```bash
# Genera un error no manejado - debe escribir en logs/error/
GET http://localhost:5000/api/test/error

# Verifica el log de error
tail -f Chubb.Bot.AI.Assistant.Api/logs/error/error-*.log
```

**Qué esperar:**
- Error capturado por el middleware
- Log escrito en `logs/error/error-YYYYMMDD.log`
- Respuesta HTTP 500 con JSON de error

#### 3. Probar Error Manual con LoggingHelper
```bash
# Error capturado manualmente con LoggingHelper
GET http://localhost:5000/api/test/error-helper

# Verifica el log
tail -f Chubb.Bot.AI.Assistant.Api/logs/error/error-*.log
```

#### 4. Probar Logs de Performance
```bash
# Operación con medición de performance
GET http://localhost:5000/api/test/performance

# Verifica el log de performance
tail -f Chubb.Bot.AI.Assistant.Api/logs/performance/performance-*.log
```

**Qué esperar:**
```
[2026-02-05 12:00:00.123] Performance: TestController.TestPerformance completed in 150ms
```

#### 5. Probar Logs de Desarrollo
```bash
# Genera logs de desarrollo
GET http://localhost:5000/api/test/development

# Verifica el log de desarrollo
tail -f Chubb.Bot.AI.Assistant.Api/logs/dev/dev-*.log
```

**Qué esperar:**
- Log de texto simple
- Log de objeto serializado (JSON)
- Warning de desarrollo

#### 6. Probar Business Exception
```bash
# Business exception (NO debe ir a logs/error/)
GET http://localhost:5000/api/test/business-error

# Verifica que NO está en error/ sino en app-*.log
tail -f Chubb.Bot.AI.Assistant.Api/logs/app-*.log | grep "business"
```

**Qué esperar:**
- Respuesta HTTP con error de negocio
- Log de WARNING (no error)
- NO aparece en `logs/error/`

#### 7. Probar Todos los Logs
```bash
# Prueba completa de todos los tipos
GET http://localhost:5000/api/test/all-logs

# Verifica cada tipo de log en ventanas separadas:
# Terminal 1
tail -f Chubb.Bot.AI.Assistant.Api/logs/performance/performance-*.log

# Terminal 2
tail -f Chubb.Bot.AI.Assistant.Api/logs/dev/dev-*.log

# Terminal 3
tail -f Chubb.Bot.AI.Assistant.Api/logs/app-*.log
```

## 📋 Script de Prueba Completo

Copia y pega este script para probar todos los endpoints:

### Windows PowerShell
```powershell
# Test 1: Error no manejado (debe ir a logs/error/)
Write-Host "Test 1: Error no manejado" -ForegroundColor Yellow
Invoke-WebRequest -Uri "http://localhost:5000/api/test/error" -Method GET
Start-Sleep -Seconds 1

# Test 2: Error con LoggingHelper (debe ir a logs/error/)
Write-Host "Test 2: Error con LoggingHelper" -ForegroundColor Yellow
Invoke-WebRequest -Uri "http://localhost:5000/api/test/error-helper" -Method GET
Start-Sleep -Seconds 1

# Test 3: Performance (debe ir a logs/performance/)
Write-Host "Test 3: Performance" -ForegroundColor Yellow
Invoke-WebRequest -Uri "http://localhost:5000/api/test/performance" -Method GET
Start-Sleep -Seconds 1

# Test 4: Development logs (debe ir a logs/dev/)
Write-Host "Test 4: Development logs" -ForegroundColor Yellow
Invoke-WebRequest -Uri "http://localhost:5000/api/test/development" -Method GET
Start-Sleep -Seconds 1

# Test 5: Business error (NO debe ir a logs/error/)
Write-Host "Test 5: Business error" -ForegroundColor Yellow
Invoke-WebRequest -Uri "http://localhost:5000/api/test/business-error" -Method GET
Start-Sleep -Seconds 1

# Test 6: Todos los logs
Write-Host "Test 6: Todos los logs" -ForegroundColor Yellow
Invoke-WebRequest -Uri "http://localhost:5000/api/test/all-logs" -Method GET

Write-Host "`nPruebas completadas! Verifica las carpetas de logs:" -ForegroundColor Green
Write-Host "  - logs/error/" -ForegroundColor Cyan
Write-Host "  - logs/performance/" -ForegroundColor Cyan
Write-Host "  - logs/dev/" -ForegroundColor Cyan
```

### Linux/Mac Bash
```bash
#!/bin/bash

BASE_URL="http://localhost:5000/api/test"

echo "Test 1: Error no manejado (debe ir a logs/error/)"
curl -X GET "$BASE_URL/error" || true
sleep 1

echo -e "\nTest 2: Error con LoggingHelper (debe ir a logs/error/)"
curl -X GET "$BASE_URL/error-helper"
sleep 1

echo -e "\nTest 3: Performance (debe ir a logs/performance/)"
curl -X GET "$BASE_URL/performance"
sleep 1

echo -e "\nTest 4: Development logs (debe ir a logs/dev/)"
curl -X GET "$BASE_URL/development"
sleep 1

echo -e "\nTest 5: Business error (NO debe ir a logs/error/)"
curl -X GET "$BASE_URL/business-error" || true
sleep 1

echo -e "\nTest 6: Todos los logs"
curl -X GET "$BASE_URL/all-logs"

echo -e "\n✅ Pruebas completadas! Verifica las carpetas de logs:"
echo "  - logs/error/"
echo "  - logs/performance/"
echo "  - logs/dev/"
```

## 🔍 Verificación de Logs

### Ver logs en tiempo real

Abre **3 terminales** separadas y ejecuta en cada una:

**Terminal 1 - Errores:**
```bash
tail -f Chubb.Bot.AI.Assistant.Api/logs/error/error-$(date +%Y%m%d).log
```

**Terminal 2 - Performance:**
```bash
tail -f Chubb.Bot.AI.Assistant.Api/logs/performance/performance-$(date +%Y%m%d).log
```

**Terminal 3 - Development:**
```bash
tail -f Chubb.Bot.AI.Assistant.Api/logs/dev/dev-$(date +%Y%m%d).log
```

### Buscar en logs

```bash
# Buscar errores del día
grep "Error" logs/error/error-*.log

# Buscar operaciones lentas (>1000ms)
grep -E "completed in [0-9]{4,}ms" logs/performance/performance-*.log

# Buscar logs de un endpoint específico
grep "TestController" logs/dev/dev-*.log

# Buscar por correlation ID
grep "correlation-id-123" logs/app-*.log
```

## ✅ Checklist de Verificación

Después de ejecutar las pruebas, verifica:

- [ ] `logs/error/error-YYYYMMDD.log` existe y contiene errores de Test 1 y 2
- [ ] `logs/performance/performance-YYYYMMDD.log` existe y contiene tiempos de Test 3 y 6
- [ ] `logs/dev/dev-YYYYMMDD.log` existe y contiene logs de Test 4 y 6
- [ ] `logs/app-YYYYMMDD.log` contiene todos los logs generales
- [ ] Business errors (Test 5) NO están en `logs/error/` pero SÍ en `logs/app-*.log`
- [ ] Cada log tiene el formato correcto con timestamp, nivel y mensaje
- [ ] Logs de performance muestran el tiempo en milisegundos

## 📊 Ejemplo de Salida Esperada

### logs/error/error-YYYYMMDD.log
```
[2026-02-05 12:00:01.234 -05:00] [ERR] [Chubb.Bot.AI.Assistant.Api.Middleware.ExceptionHandlingMiddleware] [abc-123] Unhandled exception on GET /api/test/error: Exception - Este es un error de prueba para validar logs/error/
System.Exception: Este es un error de prueba para validar logs/error/
   at Chubb.Bot.AI.Assistant.Api.Controllers.TestController.TestError() in ...
```

### logs/performance/performance-YYYYMMDD.log
```
[2026-02-05 12:00:02.456] Performance: TestController.TestPerformance completed in 152ms {"OperationName": "TestController.TestPerformance", "ElapsedMilliseconds": 152, "TestType": "Performance", "Endpoint": "/api/test/performance"}
```

### logs/dev/dev-YYYYMMDD.log
```
[2026-02-05 12:00:03.789 -05:00] [INF] [Chubb.Bot.AI.Assistant.Api.Controllers.TestController] Log de desarrollo de prueba desde TestController
{"Category": "Development", "DevLog": true}

[2026-02-05 12:00:03.790 -05:00] [INF] [Chubb.Bot.AI.Assistant.Api.Controllers.TestController] Objeto de prueba
{"Category": "Development", "DevLog": true, "ObjectData": {"UserId": 12345, "Action": "TestDevelopment", ...}}
```

## 🎯 Qué Verificar Específicamente

### ExceptionHandlingMiddleware

1. **Error no manejado** → Debe aparecer en `logs/error/`
2. **Business exception** → NO debe ir a `logs/error/`, solo warning en `logs/app-*.log`
3. **Validation error** → NO debe ir a `logs/error/`, solo warning
4. **Unauthorized** → Warning, no error

### LoggingHelper

1. **LogError()** → Escribe en `logs/error/`
2. **LogPerformance()** → Escribe en `logs/performance/` con tiempo
3. **LogDevelopment()** → Escribe en `logs/dev/`
4. **LogFatal()** → Escribe en `logs/error/`

## 🧹 Limpieza (Opcional)

Para limpiar los logs de prueba:

```bash
# Eliminar logs de prueba
rm -rf Chubb.Bot.AI.Assistant.Api/logs/error/error-*.log
rm -rf Chubb.Bot.AI.Assistant.Api/logs/performance/performance-*.log
rm -rf Chubb.Bot.AI.Assistant.Api/logs/dev/dev-*.log
```

## ⚠️ IMPORTANTE

El `TestController` es **solo para pruebas de desarrollo**. Debes:

1. ✅ Usarlo para validar que el logging funciona
2. ✅ Verificar que todos los logs se escriben en las carpetas correctas
3. ❌ **NUNCA** desplegarlo a producción
4. 🗑️ Eliminar el archivo `TestController.cs` antes de producción

## 📚 Documentación Relacionada

- **LOGGING-GUIDE.md** - Guía completa de uso
- **LOGGING-SOLUTION.md** - Resumen de la solución
- **QUICK-START-LOGGING.md** - Inicio rápido

---

**Problema del ExceptionHandlingMiddleware resuelto!** ✅

Los errores ahora se escriben correctamente en `logs/error/` usando `LoggingHelper`.
