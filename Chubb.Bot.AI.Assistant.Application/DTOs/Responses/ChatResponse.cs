namespace Chubb.Bot.AI.Assistant.Application.DTOs.Responses;

public class ChatResponse
{
    public string SessionId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Source { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
