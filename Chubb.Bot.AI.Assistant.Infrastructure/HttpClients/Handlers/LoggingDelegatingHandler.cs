namespace Chubb.Bot.AI.Assistant.Infrastructure.HttpClients.Handlers;

public class LoggingDelegatingHandler : DelegatingHandler
{
    private readonly ILogger<LoggingDelegatingHandler> _logger;

    public LoggingDelegatingHandler(ILogger<LoggingDelegatingHandler> logger)
    {
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var requestStart = DateTime.UtcNow;
        _logger.LogInformation("HTTP Request: {Method} {Uri}", request.Method, request.RequestUri);

        try
        {
            var response = await base.SendAsync(request, cancellationToken);
            var duration = DateTime.UtcNow - requestStart;

            _logger.LogInformation(
                "HTTP Response: {Method} {Uri} - {StatusCode} ({Duration}ms)",
                request.Method,
                request.RequestUri,
                response.StatusCode,
                duration.TotalMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - requestStart;
            _logger.LogError(ex, "HTTP Request Failed: {Method} {Uri} ({Duration}ms)", request.Method, request.RequestUri, duration.TotalMilliseconds);
            throw;
        }
    }
}
