namespace Chubb.Bot.AI.Assistant.Core.Models;

public class Message
{
    public string MessageId { get; set; } = Guid.NewGuid().ToString();
    public string SessionId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Type { get; set; } = "UserMessage";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? BotSource { get; set; }
    public Dictionary<string, object> AdditionalData { get; set; } = new();
}
