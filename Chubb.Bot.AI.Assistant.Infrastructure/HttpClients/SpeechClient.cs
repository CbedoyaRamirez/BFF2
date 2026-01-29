using Chubb.Bot.AI.Assistant.Core.Constants;
using Chubb.Bot.AI.Assistant.Core.Exceptions;
using Chubb.Bot.AI.Assistant.Infrastructure.HttpClients.Interfaces;

namespace Chubb.Bot.AI.Assistant.Infrastructure.HttpClients;

public class SpeechClient : ISpeechClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SpeechClient> _logger;

    public SpeechClient(HttpClient httpClient, ILogger<SpeechClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<byte[]> SynthesizeSpeechAsync(string text, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/speech/tts", new { text }, cancellationToken);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsByteArrayAsync(cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error calling Speech service for TTS");
            throw new ExternalServiceException("SpeechService", ex.Message, ex, ErrorCodes.SPEECH_SERVICE_UNAVAILABLE);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Speech service timeout");
            throw new ExternalServiceException("SpeechService", "Service timeout", ex, ErrorCodes.EXTERNAL_SERVICE_TIMEOUT);
        }
    }

    public async Task<string> RecognizeSpeechAsync(byte[] audioData, CancellationToken cancellationToken = default)
    {
        try
        {
            var content = new ByteArrayContent(audioData);
            content.Headers.Add("Content-Type", "audio/wav");

            var response = await _httpClient.PostAsync("/api/speech/stt", content, cancellationToken);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error calling Speech service for STT");
            throw new ExternalServiceException("SpeechService", ex.Message, ex, ErrorCodes.SPEECH_SERVICE_UNAVAILABLE);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Speech service timeout");
            throw new ExternalServiceException("SpeechService", "Service timeout", ex, ErrorCodes.EXTERNAL_SERVICE_TIMEOUT);
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
