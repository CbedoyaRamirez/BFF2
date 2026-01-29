namespace Chubb.Bot.AI.Assistant.FAQBot.Models;

public class FAQRequest
{
    public string SessionId { get; set; } = string.Empty;
    public string Question { get; set; } = string.Empty;
    public string? Category { get; set; }
    public Dictionary<string, string>? Context { get; set; }
}
