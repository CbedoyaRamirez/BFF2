namespace Chubb.Bot.AI.Assistant.FAQBot.Models;

public class FAQResponse
{
    public string ResponseId { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public double ConfidenceScore { get; set; }
    public List<RelatedQuestion> RelatedQuestions { get; set; } = new();
    public DateTime RespondedAt { get; set; }
}

public class RelatedQuestion
{
    public string QuestionId { get; set; } = string.Empty;
    public string QuestionText { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
}
