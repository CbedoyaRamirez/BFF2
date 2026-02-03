namespace Chubb.Bot.AI.Assistant.Application.DTOs.Responses;

public class SpeechToTextResponse
{
    public required string Text { get; set; }
    public double? Confidence { get; set; }
    public double? DurationSeconds { get; set; }
    public string? DetectedLanguage { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}
