namespace Chubb.Bot.AI.Assistant.Application.DTOs.Requests;

public class TextToSpeechRequest
{
    public string Text { get; set; } = string.Empty;
    public string? SessionId { get; set; }
}
