namespace Chubb.Bot.AI.Assistant.Application.DTOs.Responses;

public class FAQResponse
{
    public required string SessionId { get; set; }
    public required string Response { get; set; }
    public List<string> Sources { get; set; } = new();
    public int RetrievedChunks { get; set; }
}
