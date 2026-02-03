namespace Chubb.Bot.AI.Assistant.Application.DTOs.Requests;

public class FAQRequest
{
    public string Question { get; set; } = string.Empty;
    public string? SessionId { get; set; }
}
