using Microsoft.AspNetCore.Mvc;
using Chubb.Bot.AI.Assistant.FAQBot.Models;

namespace Chubb.Bot.AI.Assistant.FAQBot.Controllers;

[ApiController]
[Route("api/faq")]
public class FAQController : ControllerBase
{
    private readonly ILogger<FAQController> _logger;

    // Sample FAQ database
    private static readonly Dictionary<string, (string Answer, string Category)> FAQDatabase = new()
    {
        { "coverage", ("Our insurance covers auto, home, life, and business insurance with comprehensive protection.", "General") },
        { "claim", ("To file a claim, log into your account and go to the Claims section, or call our 24/7 hotline.", "Claims") },
        { "premium", ("Premiums are calculated based on risk factors, coverage amount, and your personal profile.", "Pricing") },
        { "cancel", ("You can cancel your policy anytime by contacting customer service. Refunds depend on policy terms.", "Policy Management") },
        { "contact", ("Contact us at 1-800-CHUBB or email support@chubb.com. We're available 24/7.", "Support") }
    };

    public FAQController(ILogger<FAQController> logger)
    {
        _logger = logger;
    }

    [HttpPost("query")]
    public IActionResult QueryFAQ([FromBody] FAQRequest request)
    {
        _logger.LogInformation("Processing FAQ query for session {SessionId}: {Question}",
            request.SessionId, request.Question);

        // Simple keyword matching
        var matchedKey = FAQDatabase.Keys
            .FirstOrDefault(key => request.Question.ToLower().Contains(key));

        var (answer, category) = matchedKey != null
            ? FAQDatabase[matchedKey]
            : ("I'm not sure about that. Please contact our customer service for more information.", "General");

        var response = new FAQResponse
        {
            ResponseId = Guid.NewGuid().ToString(),
            SessionId = request.SessionId,
            Question = request.Question,
            Answer = answer,
            Category = category,
            ConfidenceScore = matchedKey != null ? Random.Shared.NextDouble() * 0.3 + 0.7 : 0.5,
            RespondedAt = DateTime.UtcNow,
            RelatedQuestions = new List<RelatedQuestion>
            {
                new RelatedQuestion
                {
                    QuestionId = Guid.NewGuid().ToString(),
                    QuestionText = "What types of insurance do you offer?",
                    Category = "General"
                },
                new RelatedQuestion
                {
                    QuestionId = Guid.NewGuid().ToString(),
                    QuestionText = "How do I file a claim?",
                    Category = "Claims"
                }
            }
        };

        _logger.LogInformation("FAQ response generated: {ResponseId} with confidence {Confidence}",
            response.ResponseId, response.ConfidenceScore);

        return Ok(response);
    }

    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { status = "Healthy", service = "FAQBot", timestamp = DateTime.UtcNow });
    }
}
