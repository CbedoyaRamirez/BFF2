using Microsoft.Extensions.Diagnostics.HealthChecks;
using Chubb.Bot.AI.Assistant.Infrastructure.Redis;

namespace Chubb.Bot.AI.Assistant.Infrastructure.HealthChecks;

public class RedisHealthCheck : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var db = RedisConnectionFactory.GetDatabase();
            var latency = await db.PingAsync();

            if (latency.TotalMilliseconds > 100)
            {
                return HealthCheckResult.Degraded($"Redis latency is {latency.TotalMilliseconds}ms");
            }

            return HealthCheckResult.Healthy($"Redis is healthy (latency: {latency.TotalMilliseconds}ms)");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Redis is unhealthy", ex);
        }
    }
}
