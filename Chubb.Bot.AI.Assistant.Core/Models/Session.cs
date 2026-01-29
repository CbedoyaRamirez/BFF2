namespace Chubb.Bot.AI.Assistant.Core.Models;

public class Session
{
    public string SessionId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime LastAccessedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
    public string Status { get; set; } = "Active";
}
