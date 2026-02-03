using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Chubb.Bot.AI.Assistant.Infrastructure.HealthChecks;

public class ExternalServiceHealthCheck : IHealthCheck
{
    private readonly HttpClient _httpClient;
    private readonly string _serviceName;
    private readonly string _healthEndpoint;
    private readonly ILogger<ExternalServiceHealthCheck> _logger;

    public ExternalServiceHealthCheck(
        HttpClient httpClient,
        string serviceName,
        string healthEndpoint,
        ILogger<ExternalServiceHealthCheck> logger)
    {
        _httpClient = httpClient;
        _serviceName = serviceName;
        _healthEndpoint = healthEndpoint;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await _httpClient.GetAsync(_healthEndpoint, cancellationToken);
            stopwatch.Stop();

            var data = new Dictionary<string, object>
            {
                { "service", _serviceName },
                { "endpoint", _healthEndpoint },
                { "responseTime", $"{stopwatch.ElapsedMilliseconds}ms" },
                { "statusCode", (int)response.StatusCode }
            };

            if (response.IsSuccessStatusCode)
            {
                if (stopwatch.ElapsedMilliseconds > 1000)
                {
                    _logger.LogWarning("{ServiceName} health check is slow: {ResponseTime}ms", _serviceName, stopwatch.ElapsedMilliseconds);
                    return HealthCheckResult.Degraded(
                        $"{_serviceName} is responding slowly ({stopwatch.ElapsedMilliseconds}ms)",
                        data: data);
                }

                return HealthCheckResult.Healthy(
                    $"{_serviceName} is healthy (response time: {stopwatch.ElapsedMilliseconds}ms)",
                    data: data);
            }

            _logger.LogError("{ServiceName} health check failed with status code: {StatusCode}", _serviceName, response.StatusCode);
            return HealthCheckResult.Unhealthy(
                $"{_serviceName} returned status code {response.StatusCode}",
                data: data);
        }
        catch (TaskCanceledException ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "{ServiceName} health check timed out after {ElapsedTime}ms", _serviceName, stopwatch.ElapsedMilliseconds);

            return HealthCheckResult.Unhealthy(
                $"{_serviceName} health check timed out",
                ex,
                new Dictionary<string, object>
                {
                    { "service", _serviceName },
                    { "endpoint", _healthEndpoint },
                    { "elapsedTime", $"{stopwatch.ElapsedMilliseconds}ms" }
                });
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "{ServiceName} health check failed", _serviceName);

            return HealthCheckResult.Unhealthy(
                $"{_serviceName} is unhealthy",
                ex,
                new Dictionary<string, object>
                {
                    { "service", _serviceName },
                    { "endpoint", _healthEndpoint },
                    { "error", ex.Message }
                });
        }
    }
}
