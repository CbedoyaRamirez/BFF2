using System.Text.Json;
using Chubb.Bot.AI.Assistant.Application.DTOs.Requests;
using Chubb.Bot.AI.Assistant.Application.DTOs.Responses;
using Chubb.Bot.AI.Assistant.Core.Constants;
using Chubb.Bot.AI.Assistant.Core.Exceptions;
using Chubb.Bot.AI.Assistant.Infrastructure.HttpClients.Interfaces;

namespace Chubb.Bot.AI.Assistant.Infrastructure.HttpClients;

public class ChatBotClient : IChatBotClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ChatBotClient> _logger;

    public ChatBotClient(HttpClient httpClient, ILogger<ChatBotClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<ChatResponse> SendMessageAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/chat", request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<ChatResponse>(cancellationToken);

            if (result == null)
            {
                throw new ExternalServiceException("ChatBot", "Empty response from service", null, ErrorCodes.EXTERNAL_SERVICE_ERROR);
            }

            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error calling ChatBot service");
            throw new ExternalServiceException("ChatBot", ex.Message, ex, ErrorCodes.CHATBOT_UNAVAILABLE);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "ChatBot service timeout");
            throw new ExternalServiceException("ChatBot", "Service timeout", ex, ErrorCodes.EXTERNAL_SERVICE_TIMEOUT);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Error deserializing ChatBot response");
            throw new ExternalServiceException("ChatBot", "Invalid response format", ex, ErrorCodes.EXTERNAL_SERVICE_ERROR);
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
