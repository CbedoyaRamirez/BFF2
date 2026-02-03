using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Chubb.Bot.AI.Assistant.Infrastructure.Redis;

namespace Chubb.Bot.AI.Assistant.Infrastructure.HealthChecks;

public class RedisHealthCheck : IHealthCheck
{
    private readonly ILogger<RedisHealthCheck> _logger;

    public RedisHealthCheck(ILogger<RedisHealthCheck> logger)
    {
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var db = RedisConnectionFactory.GetDatabase();
            var connection = RedisConnectionFactory.Connection;

            // Ping test
            var latency = await db.PingAsync();

            // Get server info
            var endpoints = connection.GetEndPoints();
            var server = connection.GetServer(endpoints.First());
            var info = server.Info("Stats");

            var totalConnectionsReceived = info.FirstOrDefault(x => x.Key == "total_connections_received")?.FirstOrDefault().Value ?? "N/A";
            var connectedClients = info.FirstOrDefault(x => x.Key == "connected_clients")?.FirstOrDefault().Value ?? "N/A";

            var data = new Dictionary<string, object>
            {
                { "latency", $"{latency.TotalMilliseconds:F2}ms" },
                { "connectedClients", connectedClients },
                { "totalConnectionsReceived", totalConnectionsReceived },
                { "endpoint", endpoints.First().ToString() ?? "N/A" }
            };

            if (latency.TotalMilliseconds > 200)
            {
                _logger.LogWarning("Redis latency is high: {Latency}ms", latency.TotalMilliseconds);
                return HealthCheckResult.Degraded(
                    $"Redis latency is high ({latency.TotalMilliseconds:F2}ms)",
                    data: data);
            }

            if (latency.TotalMilliseconds > 100)
            {
                return HealthCheckResult.Degraded(
                    $"Redis latency is elevated ({latency.TotalMilliseconds:F2}ms)",
                    data: data);
            }

            return HealthCheckResult.Healthy(
                $"Redis is healthy (latency: {latency.TotalMilliseconds:F2}ms)",
                data: data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis health check failed");
            return HealthCheckResult.Unhealthy(
                "Redis is unhealthy",
                ex,
                new Dictionary<string, object> { { "error", ex.Message } });
        }
    }
}
