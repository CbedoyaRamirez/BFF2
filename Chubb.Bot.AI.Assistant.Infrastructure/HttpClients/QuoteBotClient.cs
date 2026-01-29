using System.Text.Json;
using Chubb.Bot.AI.Assistant.Core.Constants;
using Chubb.Bot.AI.Assistant.Core.Exceptions;
using Chubb.Bot.AI.Assistant.Infrastructure.HttpClients.Interfaces;

namespace Chubb.Bot.AI.Assistant.Infrastructure.HttpClients;

public class QuoteBotClient : IQuoteBotClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<QuoteBotClient> _logger;

    public QuoteBotClient(HttpClient httpClient, ILogger<QuoteBotClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<string> GetQuoteAsync(string query, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/quote/generate", new { query }, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadAsStringAsync(cancellationToken);
            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error calling QuoteBot service");
            throw new ExternalServiceException("QuoteBot", ex.Message, ex, ErrorCodes.QUOTEBOT_UNAVAILABLE);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "QuoteBot service timeout");
            throw new ExternalServiceException("QuoteBot", "Service timeout", ex, ErrorCodes.EXTERNAL_SERVICE_TIMEOUT);
        }
    }

    public async Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/health", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
