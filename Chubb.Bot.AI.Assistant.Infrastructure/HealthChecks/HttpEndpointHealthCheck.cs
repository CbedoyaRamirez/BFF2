using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Diagnostics;
using System.Text.Json;

namespace Chubb.Bot.AI.Assistant.Infrastructure.HealthChecks;

/// <summary>
/// Health check mejorado para endpoints HTTP externos
/// Proporciona información detallada sobre el estado de los microservicios
/// </summary>
public class HttpEndpointHealthCheck : IHealthCheck
{
    private readonly HttpClient _httpClient;
    private readonly string _url;
    private readonly string _serviceName;

    public HttpEndpointHealthCheck(HttpClient httpClient, string url, string serviceName)
    {
        _httpClient = httpClient;
        _url = url;
        _serviceName = serviceName;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var data = new Dictionary<string, object>
        {
            { "url", _url },
            { "service", _serviceName }
        };

        try
        {
            // Realizar request con timeout explícito
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(5));

            var response = await _httpClient.GetAsync(_url, cts.Token);
            stopwatch.Stop();

            data["responseTime"] = $"{stopwatch.ElapsedMilliseconds}ms";
            data["statusCode"] = (int)response.StatusCode;

            // Intentar leer respuesta JSON del microservicio
            if (response.IsSuccessStatusCode && response.Content.Headers.ContentType?.MediaType == "application/json")
            {
                try
                {
                    var content = await response.Content.ReadAsStringAsync(cancellationToken);
                    var serviceHealth = JsonSerializer.Deserialize<ServiceHealthResponse>(content);

                    if (serviceHealth != null)
                    {
                        data["serviceVersion"] = serviceHealth.Version ?? "unknown";
                        data["serviceStatus"] = serviceHealth.Status ?? "unknown";

                        if (serviceHealth.Checks != null && serviceHealth.Checks.Any())
                        {
                            data["serviceChecks"] = string.Join(", ",
                                serviceHealth.Checks.Select(c => $"{c.Name}: {c.Status}"));
                        }
                    }
                }
                catch
                {
                    // Si no se puede parsear, ignorar
                }
            }

            // Determinar el estado según el tiempo de respuesta y status code
            if (!response.IsSuccessStatusCode)
            {
                return HealthCheckResult.Unhealthy(
                    $"{_serviceName} returned status code {response.StatusCode}",
                    data: data);
            }

            if (stopwatch.ElapsedMilliseconds > 3000)
            {
                return HealthCheckResult.Degraded(
                    $"{_serviceName} is responding slowly ({stopwatch.ElapsedMilliseconds}ms)",
                    data: data);
            }

            if (stopwatch.ElapsedMilliseconds > 1000)
            {
                return HealthCheckResult.Degraded(
                    $"{_serviceName} response time is elevated ({stopwatch.ElapsedMilliseconds}ms)",
                    data: data);
            }

            return HealthCheckResult.Healthy(
                $"{_serviceName} is responding normally",
                data: data);
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            data["responseTime"] = $"{stopwatch.ElapsedMilliseconds}ms (timeout)";
            data["error"] = "Request timeout after 5 seconds";

            return HealthCheckResult.Unhealthy(
                $"{_serviceName} health check timed out",
                data: data);
        }
        catch (HttpRequestException ex)
        {
            stopwatch.Stop();
            data["responseTime"] = $"{stopwatch.ElapsedMilliseconds}ms";
            data["error"] = ex.Message;
            data["errorType"] = ex.GetType().Name;

            // Determinar si es un problema de conexión o DNS
            if (ex.InnerException != null)
            {
                data["innerError"] = ex.InnerException.Message;
            }

            return HealthCheckResult.Unhealthy(
                $"{_serviceName} is unavailable: {ex.Message}",
                exception: ex,
                data: data);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            data["responseTime"] = $"{stopwatch.ElapsedMilliseconds}ms";
            data["error"] = ex.Message;
            data["errorType"] = ex.GetType().Name;

            return HealthCheckResult.Unhealthy(
                $"{_serviceName} health check failed: {ex.Message}",
                exception: ex,
                data: data);
        }
    }

    // Clase para deserializar respuesta de health check de microservicios
    private class ServiceHealthResponse
    {
        public string? Status { get; set; }
        public string? Service { get; set; }
        public string? Version { get; set; }
        public List<ServiceCheck>? Checks { get; set; }
    }

    private class ServiceCheck
    {
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}
