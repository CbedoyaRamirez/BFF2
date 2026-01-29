namespace Chubb.Bot.AI.Assistant.Application.DTOs.Requests;

public class ChatRequest
{
    public string SessionId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
