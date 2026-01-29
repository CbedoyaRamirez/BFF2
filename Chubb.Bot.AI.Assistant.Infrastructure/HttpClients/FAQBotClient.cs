using Chubb.Bot.AI.Assistant.Core.Constants;
using Chubb.Bot.AI.Assistant.Core.Exceptions;
using Chubb.Bot.AI.Assistant.Infrastructure.HttpClients.Interfaces;

namespace Chubb.Bot.AI.Assistant.Infrastructure.HttpClients;

public class FAQBotClient : IFAQBotClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<FAQBotClient> _logger;

    public FAQBotClient(HttpClient httpClient, ILogger<FAQBotClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<string> GetAnswerAsync(string question, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/faq/answer", new { question }, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadAsStringAsync(cancellationToken);
            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error calling FAQBot service");
            throw new ExternalServiceException("FAQBot", ex.Message, ex, ErrorCodes.FAQBOT_UNAVAILABLE);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "FAQBot service timeout");
            throw new ExternalServiceException("FAQBot", "Service timeout", ex, ErrorCodes.EXTERNAL_SERVICE_TIMEOUT);
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
