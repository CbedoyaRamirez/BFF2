#!/usr/bin/env pwsh
# Script SIMPLE: Crea las carpetas y ejecuta la aplicación

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  SOLUCIÓN RÁPIDA - CREAR CARPETAS" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# 1. Matar procesos dotnet existentes
Write-Host "[1/4] Deteniendo aplicación si está corriendo..." -ForegroundColor Yellow
taskkill /F /IM dotnet.exe 2>$null | Out-Null
Start-Sleep -Seconds 1
Write-Host "      ✓ Listo" -ForegroundColor Green

# 2. Crear carpetas MANUALMENTE
Write-Host ""
Write-Host "[2/4] Creando carpetas de logs..." -ForegroundColor Yellow

$apiDir = "Chubb.Bot.AI.Assistant.Api"
if (-not (Test-Path $apiDir)) {
    Write-Host "      ✗ No se encontró $apiDir" -ForegroundColor Red
    Write-Host "      Ejecuta este script desde la raíz del proyecto" -ForegroundColor Yellow
    exit 1
}

Push-Location $apiDir

# Crear carpetas una por una
$carpetas = @(
    "logs",
    "logs\error",
    "logs\performance",
    "logs\dev"
)

foreach ($carpeta in $carpetas) {
    if (-not (Test-Path $carpeta)) {
        New-Item -ItemType Directory -Path $carpeta -Force | Out-Null
        Write-Host "      ✓ Creado: $carpeta" -ForegroundColor Green
    } else {
        Write-Host "      ✓ Ya existe: $carpeta" -ForegroundColor Gray
    }
}

# Verificar que todas existan
Write-Host ""
Write-Host "   Verificando estructura:" -ForegroundColor Cyan
$allOk = $true
foreach ($carpeta in $carpetas) {
    if (Test-Path $carpeta) {
        Write-Host "      ✓ $carpeta" -ForegroundColor Green
    } else {
        Write-Host "      ✗ $carpeta - NO EXISTE" -ForegroundColor Red
        $allOk = $false
    }
}

if (-not $allOk) {
    Write-Host ""
    Write-Host "   ✗ Error: No se pudieron crear todas las carpetas" -ForegroundColor Red
    Pop-Location
    exit 1
}

# 3. Compilar (opcional)
Write-Host ""
Write-Host "[3/4] Compilando (opcional)..." -ForegroundColor Yellow
dotnet build --no-restore 2>&1 | Out-Null
if ($LASTEXITCODE -eq 0) {
    Write-Host "      ✓ Compilación OK" -ForegroundColor Green
} else {
    Write-Host "      ℹ No se pudo compilar (aplicación puede estar corriendo)" -ForegroundColor Gray
    Write-Host "      No te preocupes, continuamos..." -ForegroundColor Gray
}

# 4. Ejecutar
Write-Host ""
Write-Host "[4/4] Iniciando aplicación..." -ForegroundColor Yellow
Write-Host ""
Write-Host "=======================================" -ForegroundColor Cyan
Write-Host "  LAS CARPETAS YA ESTÁN CREADAS:" -ForegroundColor Green
Write-Host "=======================================" -ForegroundColor Cyan
Write-Host ""
Get-ChildItem "logs" -Directory | ForEach-Object {
    Write-Host "  ✓ logs\$($_.Name)" -ForegroundColor Green
}
Write-Host ""
Write-Host "La aplicación debería ver:" -ForegroundColor Cyan
Write-Host "  [INFO] Log directory already exists: ..." -ForegroundColor Gray
Write-Host ""
Write-Host "=======================================" -ForegroundColor Cyan
Write-Host ""

# Ejecutar aplicación
dotnet run

Pop-Location
