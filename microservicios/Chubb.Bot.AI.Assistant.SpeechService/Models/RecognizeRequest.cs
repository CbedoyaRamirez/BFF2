namespace Chubb.Bot.AI.Assistant.SpeechService.Models;

public class RecognizeRequest
{
    public string AudioUrl { get; set; } = string.Empty;
    public string? Base64Audio { get; set; }
    public string Language { get; set; } = "en-US";
    public string Format { get; set; } = "mp3";
}
