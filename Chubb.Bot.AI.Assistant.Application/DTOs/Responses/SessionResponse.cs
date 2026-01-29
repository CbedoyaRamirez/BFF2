namespace Chubb.Bot.AI.Assistant.Application.DTOs.Responses;

public class SessionResponse
{
    public string SessionId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string Status { get; set; } = "Active";
}
