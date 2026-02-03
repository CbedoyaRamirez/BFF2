# ============================================
# SCRIPT DE PRUEBA DE HEALTH CHECKS
# ============================================
# Verifica el estado de todos los servicios

$services = @(
    @{
        Name = "BFF API"
        Url = "http://localhost:5016/health"
        Color = "Cyan"
    },
    @{
        Name = "ChatBot"
        Url = "http://localhost:5266/health"
        Color = "Blue"
    },
    @{
        Name = "FAQBot"
        Url = "http://localhost:5267/health"
        Color = "Magenta"
    },
    @{
        Name = "SpeechService"
        Url = "http://localhost:7001/health"
        Color = "Yellow"
    }
)

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "  HEALTH CHECK - TODOS LOS SERVICIOS" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""

$results = @()

foreach ($service in $services) {
    Write-Host "Verificando $($service.Name)..." -ForegroundColor $service.Color

    try {
        $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
        $response = Invoke-RestMethod -Uri $service.Url -Method Get -TimeoutSec 10
        $stopwatch.Stop()

        $status = $response.status
        $responseTime = $stopwatch.ElapsedMilliseconds

        $result = @{
            Service = $service.Name
            Status = $status
            ResponseTime = $responseTime
            Available = $true
        }

        # Colorear según el estado
        if ($status -eq "Healthy") {
            Write-Host "  ✓ Status: $status" -ForegroundColor Green
            Write-Host "  ⏱ Response Time: ${responseTime}ms" -ForegroundColor Gray
        }
        elseif ($status -eq "Degraded") {
            Write-Host "  ⚠ Status: $status" -ForegroundColor Yellow
            Write-Host "  ⏱ Response Time: ${responseTime}ms" -ForegroundColor Gray
        }
        else {
            Write-Host "  ✗ Status: $status" -ForegroundColor Red
            Write-Host "  ⏱ Response Time: ${responseTime}ms" -ForegroundColor Gray
        }

        # Mostrar información adicional si está disponible
        if ($response.service) {
            Write-Host "  📦 Service: $($response.service)" -ForegroundColor Gray
        }
        if ($response.version) {
            Write-Host "  🏷 Version: $($response.version)" -ForegroundColor Gray
        }

        # Mostrar checks individuales si existen
        if ($response.checks -and $response.checks.Count -gt 0) {
            Write-Host "  Checks:" -ForegroundColor Gray
            foreach ($check in $response.checks) {
                $checkStatus = $check.status
                $checkSymbol = if ($checkStatus -eq "Healthy") { "✓" } elseif ($checkStatus -eq "Degraded") { "⚠" } else { "✗" }
                $checkColor = if ($checkStatus -eq "Healthy") { "Green" } elseif ($checkStatus -eq "Degraded") { "Yellow" } else { "Red" }
                Write-Host "    $checkSymbol $($check.name): $checkStatus" -ForegroundColor $checkColor
            }
        }

        # Mostrar entries si existen (BFF API)
        if ($response.entries) {
            Write-Host "  External Services:" -ForegroundColor Gray
            foreach ($entry in $response.entries.PSObject.Properties) {
                $entryName = $entry.Name
                $entryValue = $entry.Value
                $entryStatus = $entryValue.status

                if ($entryName -ne "self") {
                    $entrySymbol = if ($entryStatus -eq "Healthy") { "✓" } elseif ($entryStatus -eq "Degraded") { "⚠" } else { "✗" }
                    $entryColor = if ($entryStatus -eq "Healthy") { "Green" } elseif ($entryStatus -eq "Degraded") { "Yellow" } else { "Red" }
                    Write-Host "    $entrySymbol $entryName: $entryStatus" -ForegroundColor $entryColor

                    if ($entryValue.data.responseTime) {
                        Write-Host "       Response Time: $($entryValue.data.responseTime)" -ForegroundColor DarkGray
                    }
                }
            }
        }

        $results += $result
    }
    catch {
        Write-Host "  ✗ Status: UNAVAILABLE" -ForegroundColor Red
        Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor DarkRed

        $result = @{
            Service = $service.Name
            Status = "Unavailable"
            ResponseTime = 0
            Available = $false
            Error = $_.Exception.Message
        }

        $results += $result
    }

    Write-Host ""
}

# Resumen
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "  RESUMEN" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan

$availableCount = ($results | Where-Object { $_.Available -eq $true }).Count
$healthyCount = ($results | Where-Object { $_.Status -eq "Healthy" }).Count
$degradedCount = ($results | Where-Object { $_.Status -eq "Degraded" }).Count
$unavailableCount = ($results | Where-Object { $_.Available -eq $false }).Count

Write-Host "Total Services:      $($services.Count)" -ForegroundColor White
Write-Host "✓ Healthy:           $healthyCount" -ForegroundColor Green
Write-Host "⚠ Degraded:          $degradedCount" -ForegroundColor Yellow
Write-Host "✗ Unavailable:       $unavailableCount" -ForegroundColor Red
Write-Host ""

# Tabla de resultados
Write-Host "Detalle de Servicios:" -ForegroundColor White
Write-Host "─────────────────────────────────────────────────────" -ForegroundColor Gray
Write-Host ("{0,-20} {1,-15} {2,10}" -f "Service", "Status", "Response") -ForegroundColor White
Write-Host "─────────────────────────────────────────────────────" -ForegroundColor Gray

foreach ($result in $results) {
    $statusColor = if ($result.Status -eq "Healthy") { "Green" }
                   elseif ($result.Status -eq "Degraded") { "Yellow" }
                   else { "Red" }

    $responseTimeStr = if ($result.Available) { "$($result.ResponseTime)ms" } else { "N/A" }

    Write-Host ("{0,-20}" -f $result.Service) -NoNewline
    Write-Host ("{0,-15}" -f $result.Status) -ForegroundColor $statusColor -NoNewline
    Write-Host ("{0,10}" -f $responseTimeStr) -ForegroundColor Gray
}

Write-Host "─────────────────────────────────────────────────────" -ForegroundColor Gray
Write-Host ""

# Evaluación final
if ($unavailableCount -eq 0 -and $degradedCount -eq 0) {
    Write-Host "✓ TODOS LOS SERVICIOS ESTÁN HEALTHY!" -ForegroundColor Green
}
elseif ($unavailableCount -eq 0 -and $degradedCount -gt 0) {
    Write-Host "⚠ ALGUNOS SERVICIOS ESTÁN DEGRADED" -ForegroundColor Yellow
    Write-Host "  Los servicios están disponibles pero con performance reducido" -ForegroundColor Yellow
}
elseif ($unavailableCount -gt 0) {
    Write-Host "✗ ALGUNOS SERVICIOS NO ESTÁN DISPONIBLES" -ForegroundColor Red
    Write-Host "  Por favor verifica que todos los servicios estén corriendo" -ForegroundColor Red
}

Write-Host ""
Write-Host "Presiona cualquier tecla para salir..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
