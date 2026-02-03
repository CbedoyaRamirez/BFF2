namespace Chubb.Bot.AI.Assistant.Application.DTOs.Responses;

public class TextToSpeechResponse
{
    public byte[] AudioData { get; set; } = Array.Empty<byte>();
    public string ContentType { get; set; } = "audio/wav";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
