namespace Chubb.Bot.AI.Assistant.SpeechService.Models;

public class SynthesizeRequest
{
    public string Text { get; set; } = string.Empty;
    public string Language { get; set; } = "en-US";
    public string Voice { get; set; } = "female";
    public double Speed { get; set; } = 1.0;
    public string Format { get; set; } = "mp3";
}
