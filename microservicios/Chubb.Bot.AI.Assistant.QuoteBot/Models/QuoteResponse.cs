namespace Chubb.Bot.AI.Assistant.QuoteBot.Models;

public class QuoteResponse
{
    public string QuoteId { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public decimal EstimatedPrice { get; set; }
    public string Currency { get; set; } = "USD";
    public List<QuoteItem> Items { get; set; } = new();
    public DateTime GeneratedAt { get; set; }
}

public class QuoteItem
{
    public string ProductName { get; set; } = string.Empty;
    public string Coverage { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Description { get; set; } = string.Empty;
}
