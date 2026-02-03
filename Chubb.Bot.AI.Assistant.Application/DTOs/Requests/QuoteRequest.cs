namespace Chubb.Bot.AI.Assistant.Application.DTOs.Requests;

public class QuoteRequest
{
    public string Query { get; set; } = string.Empty;
    public string? SessionId { get; set; }
}
