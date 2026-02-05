#!/usr/bin/env pwsh
# Script de diagnóstico para verificar la creación de carpetas

Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  DIAGNÓSTICO DE CARPETAS DE LOGS" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# Ir al directorio de la API
$apiDir = "Chubb.Bot.AI.Assistant.Api"
$currentDir = Get-Location

Write-Host "Directorio actual: $currentDir" -ForegroundColor Gray
Write-Host ""

if (Test-Path $apiDir) {
    Push-Location $apiDir
    $apiPath = Get-Location
    Write-Host "✓ Directorio API encontrado: $apiPath" -ForegroundColor Green
} else {
    Write-Host "✗ No se encontró el directorio $apiDir" -ForegroundColor Red
    Write-Host "  Ejecuta este script desde la raíz del proyecto" -ForegroundColor Yellow
    exit 1
}

Write-Host ""
Write-Host "--- PASO 1: Verificar permisos de escritura ---" -ForegroundColor Yellow
try {
    $testFile = "test-permisos.tmp"
    "test" | Out-File $testFile
    Remove-Item $testFile
    Write-Host "✓ Permisos de escritura: OK" -ForegroundColor Green
} catch {
    Write-Host "✗ Error de permisos de escritura" -ForegroundColor Red
    Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Red
    Pop-Location
    exit 1
}

Write-Host ""
Write-Host "--- PASO 2: Intentar crear carpetas manualmente ---" -ForegroundColor Yellow

$carpetas = @(
    "logs",
    "logs\error",
    "logs\performance",
    "logs\dev"
)

foreach ($carpeta in $carpetas) {
    Write-Host ""
    Write-Host "Procesando: $carpeta" -ForegroundColor Cyan

    if (Test-Path $carpeta) {
        Write-Host "  ℹ Ya existe" -ForegroundColor Gray

        # Verificar si es un directorio
        $item = Get-Item $carpeta
        if ($item.PSIsContainer) {
            Write-Host "  ✓ Es un directorio válido" -ForegroundColor Green
        } else {
            Write-Host "  ✗ NO es un directorio (¿archivo?)" -ForegroundColor Red
        }
    } else {
        Write-Host "  → Intentando crear..." -ForegroundColor Yellow
        try {
            New-Item -ItemType Directory -Path $carpeta -Force | Out-Null
            Write-Host "  ✓ Creado exitosamente" -ForegroundColor Green
        } catch {
            Write-Host "  ✗ Error al crear" -ForegroundColor Red
            Write-Host "    Error: $($_.Exception.Message)" -ForegroundColor Red
        }
    }
}

Write-Host ""
Write-Host "--- PASO 3: Verificar estructura final ---" -ForegroundColor Yellow
Write-Host ""

if (Test-Path "logs") {
    Write-Host "Contenido de 'logs\':" -ForegroundColor Cyan
    Get-ChildItem -Path "logs" -Directory | ForEach-Object {
        Write-Host "  ✓ $($_.Name)" -ForegroundColor Green
    }

    # Verificar cada subcarpeta
    Write-Host ""
    $subcarpetas = @("error", "performance", "dev")
    foreach ($sub in $subcarpetas) {
        $path = "logs\$sub"
        if (Test-Path $path) {
            Write-Host "  ✓ logs\$sub existe" -ForegroundColor Green
        } else {
            Write-Host "  ✗ logs\$sub NO existe" -ForegroundColor Red
        }
    }
} else {
    Write-Host "✗ La carpeta 'logs' no existe" -ForegroundColor Red
}

Write-Host ""
Write-Host "--- PASO 4: Información del sistema ---" -ForegroundColor Yellow
Write-Host ""
Write-Host "Sistema Operativo: $([System.Environment]::OSVersion)" -ForegroundColor Gray
Write-Host "Usuario: $([System.Environment]::UserName)" -ForegroundColor Gray
Write-Host "Ruta completa: $(Get-Location)" -ForegroundColor Gray

Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  DIAGNÓSTICO COMPLETADO" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# Verificar si todas las carpetas necesarias existen
$allExist = $true
foreach ($sub in @("error", "performance", "dev")) {
    if (-not (Test-Path "logs\$sub")) {
        $allExist = $false
        break
    }
}

if ($allExist) {
    Write-Host "✓ RESULTADO: Todas las carpetas existen correctamente" -ForegroundColor Green
    Write-Host ""
    Write-Host "Ahora puedes iniciar la aplicación:" -ForegroundColor Cyan
    Write-Host "  dotnet run" -ForegroundColor White
} else {
    Write-Host "✗ RESULTADO: Algunas carpetas NO se pudieron crear" -ForegroundColor Red
    Write-Host ""
    Write-Host "Posibles causas:" -ForegroundColor Yellow
    Write-Host "  1. Permisos insuficientes" -ForegroundColor Gray
    Write-Host "  2. Antivirus bloqueando" -ForegroundColor Gray
    Write-Host "  3. Disco lleno" -ForegroundColor Gray
}

Write-Host ""
Pop-Location
