namespace Chubb.Bot.AI.Assistant.Infrastructure.HttpClients.Interfaces;

public interface ISpeechClient
{
    Task<byte[]> SynthesizeSpeechAsync(string text, CancellationToken cancellationToken = default);
    Task<string> RecognizeSpeechAsync(byte[] audioData, CancellationToken cancellationToken = default);
    Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default);
}
