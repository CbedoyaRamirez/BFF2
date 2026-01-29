namespace Chubb.Bot.AI.Assistant.QuoteBot.Models;

public class QuoteRequest
{
    public string SessionId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public Dictionary<string, string>? Context { get; set; }
}
