using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;
using Polly.Timeout;

namespace Chubb.Bot.AI.Assistant.Infrastructure.Policies;

public static class PollyPolicies
{
    public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(int retryCount = 3, ILogger? logger = null)
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .Or<TimeoutRejectedException>()
            .WaitAndRetryAsync(
                retryCount,
                retryAttempt => TimeSpan.FromMilliseconds(Math.Pow(2, retryAttempt) * 100),
                onRetry: (outcome, timespan, retryAttempt, context) =>
                {
                    var message = $"Retry {retryAttempt}/{retryCount} after {timespan.TotalMilliseconds}ms";
                    var reason = outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString() ?? "Unknown";

                    if (logger != null)
                    {
                        logger.LogWarning("HTTP Retry: {Message}. Reason: {Reason}", message, reason);
                    }
                    else
                    {
                        Console.WriteLine($"{message} due to: {reason}");
                    }
                });
    }

    public static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy(int failureThreshold = 5, int durationSeconds = 30, ILogger? logger = null)
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: failureThreshold,
                durationOfBreak: TimeSpan.FromSeconds(durationSeconds),
                onBreak: (outcome, duration) =>
                {
                    var reason = outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString() ?? "Unknown";

                    if (logger != null)
                    {
                        logger.LogError("Circuit breaker opened for {Duration}s. Reason: {Reason}", duration.TotalSeconds, reason);
                    }
                    else
                    {
                        Console.WriteLine($"Circuit breaker opened for {duration.TotalSeconds}s due to: {reason}");
                    }
                },
                onReset: () =>
                {
                    if (logger != null)
                    {
                        logger.LogInformation("Circuit breaker reset - service recovered");
                    }
                    else
                    {
                        Console.WriteLine("Circuit breaker reset");
                    }
                },
                onHalfOpen: () =>
                {
                    if (logger != null)
                    {
                        logger.LogInformation("Circuit breaker half-open - testing service");
                    }
                });
    }

    public static IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy(int timeoutSeconds = 10, ILogger? logger = null)
    {
        return Policy.TimeoutAsync<HttpResponseMessage>(
            TimeSpan.FromSeconds(timeoutSeconds),
            onTimeoutAsync: (context, timespan, task) =>
            {
                if (logger != null)
                {
                    logger.LogWarning("Request timeout after {Timeout}s", timespan.TotalSeconds);
                }
                return Task.CompletedTask;
            });
    }

    public static IAsyncPolicy<HttpResponseMessage> GetCombinedPolicy(
        int retryCount = 3,
        int circuitBreakerThreshold = 5,
        int circuitBreakerDuration = 30,
        int timeoutSeconds = 10,
        ILogger? logger = null)
    {
        var retryPolicy = GetRetryPolicy(retryCount, logger);
        var circuitBreakerPolicy = GetCircuitBreakerPolicy(circuitBreakerThreshold, circuitBreakerDuration, logger);
        var timeoutPolicy = GetTimeoutPolicy(timeoutSeconds, logger);

        return Policy.WrapAsync(retryPolicy, circuitBreakerPolicy, timeoutPolicy);
    }
}
