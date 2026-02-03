namespace Chubb.Bot.AI.Assistant.Application.DTOs.Responses;

public class QuoteResponse
{
    public string Quote { get; set; } = string.Empty;
    public string? SessionId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
