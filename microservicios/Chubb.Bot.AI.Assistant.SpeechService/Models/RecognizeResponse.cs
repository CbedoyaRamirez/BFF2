namespace Chubb.Bot.AI.Assistant.SpeechService.Models;

public class RecognizeResponse
{
    public string TranscriptionId { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public string Language { get; set; } = string.Empty;
    public int DurationSeconds { get; set; }
    public List<Word> Words { get; set; } = new();
    public DateTime ProcessedAt { get; set; }
}

public class Word
{
    public string Text { get; set; } = string.Empty;
    public double StartTime { get; set; }
    public double EndTime { get; set; }
    public double Confidence { get; set; }
}
