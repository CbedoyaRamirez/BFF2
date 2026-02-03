using System.Text.Json;
using Chubb.Bot.AI.Assistant.Application.DTOs.Requests;
using Chubb.Bot.AI.Assistant.Application.DTOs.Responses;
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

    public async Task<FAQResponse> GetAnswerAsync(FAQRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/faq", request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<FAQResponse>(cancellationToken);

            if (result == null)
            {
                throw new ExternalServiceException("FAQBot", "Empty response from service", null, ErrorCodes.EXTERNAL_SERVICE_ERROR);
            }

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
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Error deserializing FAQBot response");
            throw new ExternalServiceException("FAQBot", "Invalid response format", ex, ErrorCodes.EXTERNAL_SERVICE_ERROR);
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
