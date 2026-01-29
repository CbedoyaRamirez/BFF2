namespace Chubb.Bot.AI.Assistant.Core.Models;

public class Conversation
{
    public string ConversationId { get; set; } = Guid.NewGuid().ToString();
    public string SessionId { get; set; } = string.Empty;
    public List<Message> Messages { get; set; } = new();
    public Dictionary<string, object> Context { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;
}
