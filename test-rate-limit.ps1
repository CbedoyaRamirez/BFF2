# ============================================
# SCRIPT DE PRUEBA DE RATE LIMITING
# ============================================
# Este script envía múltiples requests al API para probar el rate limiting
# Límite configurado: 60 requests por minuto

$host_url = "http://localhost:5016"
$endpoint = "/health"
$total_requests = 80
$delay_ms = 100  # Delay entre requests en milisegundos

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "  PRUEBA DE RATE LIMITING - BFF API" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "URL: $host_url$endpoint" -ForegroundColor Yellow
Write-Host "Total Requests: $total_requests" -ForegroundColor Yellow
Write-Host "Delay: $delay_ms ms" -ForegroundColor Yellow
Write-Host "Límite esperado: 60 requests/minuto" -ForegroundColor Yellow
Write-Host ""
Write-Host "Iniciando prueba..." -ForegroundColor Green
Write-Host ""

$success_count = 0
$rate_limited_count = 0
$error_count = 0

for ($i = 1; $i -le $total_requests; $i++) {
    try {
        $response = Invoke-WebRequest -Uri "$host_url$endpoint" -Method Get -UseBasicParsing -ErrorAction Stop

        if ($response.StatusCode -eq 200) {
            $success_count++
            Write-Host "[$i/$total_requests] ✓ Status: $($response.StatusCode) OK" -ForegroundColor Green
        }
    }
    catch {
        $statusCode = $_.Exception.Response.StatusCode.value__

        if ($statusCode -eq 429) {
            $rate_limited_count++
            Write-Host "[$i/$total_requests] ⚠ Status: 429 - RATE LIMITED!" -ForegroundColor Red

            # Intentar leer el mensaje de error
            try {
                $stream = $_.Exception.Response.GetResponseStream()
                $reader = New-Object System.IO.StreamReader($stream)
                $responseBody = $reader.ReadToEnd()
                Write-Host "    Mensaje: $responseBody" -ForegroundColor DarkRed
            }
            catch {
                Write-Host "    Mensaje: Too Many Requests" -ForegroundColor DarkRed
            }
        }
        else {
            $error_count++
            Write-Host "[$i/$total_requests] ✗ Status: $statusCode - ERROR" -ForegroundColor Yellow
            Write-Host "    Error: $($_.Exception.Message)" -ForegroundColor DarkYellow
        }
    }

    # Delay entre requests
    if ($i -lt $total_requests) {
        Start-Sleep -Milliseconds $delay_ms
    }
}

Write-Host ""
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "  RESULTADOS" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "Total Requests:        $total_requests" -ForegroundColor White
Write-Host "✓ Exitosos (200):      $success_count" -ForegroundColor Green
Write-Host "⚠ Rate Limited (429):  $rate_limited_count" -ForegroundColor Red
Write-Host "✗ Errores:             $error_count" -ForegroundColor Yellow
Write-Host ""

# Validar que el rate limiting funcionó correctamente
if ($success_count -gt 0 -and $rate_limited_count -gt 0) {
    Write-Host "✓ RATE LIMITING ESTÁ FUNCIONANDO CORRECTAMENTE!" -ForegroundColor Green
    Write-Host "  - Primeros $success_count requests fueron exitosos" -ForegroundColor Green
    Write-Host "  - Siguientes $rate_limited_count requests fueron bloqueados" -ForegroundColor Green
}
elseif ($success_count -eq $total_requests) {
    Write-Host "⚠ ADVERTENCIA: Todos los requests fueron exitosos" -ForegroundColor Yellow
    Write-Host "  El rate limiting puede no estar funcionando o el límite es muy alto" -ForegroundColor Yellow
}
else {
    Write-Host "✗ ERROR: Resultados inesperados" -ForegroundColor Red
}

Write-Host ""
Write-Host "Presiona cualquier tecla para salir..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
