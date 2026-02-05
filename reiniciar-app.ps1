#!/usr/bin/env pwsh
# Script para reiniciar la aplicación y verificar que las carpetas de logs se crean

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  REINICIO DE APLICACIÓN - LOGS FIX" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# 1. Matar procesos dotnet
Write-Host "[1/5] Deteniendo aplicación existente..." -ForegroundColor Yellow
$processes = Get-Process -Name "dotnet" -ErrorAction SilentlyContinue
if ($processes) {
    taskkill /F /IM dotnet.exe 2>$null | Out-Null
    Write-Host "      ✓ Aplicación detenida" -ForegroundColor Green
    Start-Sleep -Seconds 2
} else {
    Write-Host "      ℹ No hay aplicación corriendo" -ForegroundColor Gray
}

# 2. Limpiar logs (para verificar que se crean)
Write-Host ""
Write-Host "[2/5] Limpiando carpetas de logs..." -ForegroundColor Yellow
$logDirs = @(
    "Chubb.Bot.AI.Assistant.Api\logs\error",
    "Chubb.Bot.AI.Assistant.Api\logs\performance",
    "Chubb.Bot.AI.Assistant.Api\logs\dev"
)

foreach ($dir in $logDirs) {
    if (Test-Path $dir) {
        Remove-Item -Recurse -Force $dir
        Write-Host "      ✓ Eliminado: $dir" -ForegroundColor Green
    }
}

# 3. Compilar
Write-Host ""
Write-Host "[3/5] Compilando proyecto..." -ForegroundColor Yellow
Push-Location Chubb.Bot.AI.Assistant.Api
$output = dotnet build 2>&1
$buildSuccess = $LASTEXITCODE -eq 0

if ($buildSuccess) {
    Write-Host "      ✓ Compilación exitosa" -ForegroundColor Green
} else {
    Write-Host "      ✗ Error en compilación" -ForegroundColor Red
    Write-Host ""
    Write-Host "Detalles del error:" -ForegroundColor Red
    Write-Host $output -ForegroundColor Gray
    Pop-Location
    exit 1
}

# 4. Verificar que las carpetas NO existen antes de iniciar
Write-Host ""
Write-Host "[4/5] Verificando estado inicial..." -ForegroundColor Yellow
$errorExists = Test-Path "logs\error"
$perfExists = Test-Path "logs\performance"
$devExists = Test-Path "logs\dev"

if (-not $errorExists -and -not $perfExists -and -not $devExists) {
    Write-Host "      ✓ Carpetas listas para crearse" -ForegroundColor Green
} else {
    Write-Host "      ℹ Algunas carpetas ya existen" -ForegroundColor Gray
}

# 5. Iniciar aplicación
Write-Host ""
Write-Host "[5/5] Iniciando aplicación..." -ForegroundColor Yellow
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "BUSCA ESTOS MENSAJES EN LA CONSOLA:" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "  [INFO] Created log directory: logs" -ForegroundColor White
Write-Host "  [INFO] Created log directory: logs/error" -ForegroundColor White
Write-Host "  [INFO] Created log directory: logs/performance" -ForegroundColor White
Write-Host "  [INFO] Created log directory: logs/dev" -ForegroundColor White
Write-Host ""
Write-Host "O si ya existen:" -ForegroundColor Gray
Write-Host "  [INFO] Log directory already exists: ..." -ForegroundColor Gray
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Iniciar
dotnet run

Pop-Location
