namespace Chubb.Bot.AI.Assistant.Application.DTOs.Responses;

public class SpeechToTextResponse
{
    public string Text { get; set; } = string.Empty;
    public string? SessionId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
