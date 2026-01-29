namespace Chubb.Bot.AI.Assistant.SpeechService.Models;

public class SynthesizeResponse
{
    public string AudioId { get; set; } = string.Empty;
    public string AudioUrl { get; set; } = string.Empty;
    public string Format { get; set; } = string.Empty;
    public int DurationSeconds { get; set; }
    public long FileSizeBytes { get; set; }
    public DateTime GeneratedAt { get; set; }
    public string? Base64Audio { get; set; }
}
