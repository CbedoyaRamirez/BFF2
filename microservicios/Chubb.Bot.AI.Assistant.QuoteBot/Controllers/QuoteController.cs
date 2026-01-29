using Microsoft.AspNetCore.Mvc;
using Chubb.Bot.AI.Assistant.QuoteBot.Models;

namespace Chubb.Bot.AI.Assistant.QuoteBot.Controllers;

[ApiController]
[Route("api/quote")]
public class QuoteController : ControllerBase
{
    private readonly ILogger<QuoteController> _logger;

    public QuoteController(ILogger<QuoteController> logger)
    {
        _logger = logger;
    }

    [HttpPost("generate")]
    public IActionResult GenerateQuote([FromBody] QuoteRequest request)
    {
        _logger.LogInformation("Generating quote for session {SessionId}", request.SessionId);

        // Simulate quote generation logic
        var response = new QuoteResponse
        {
            QuoteId = Guid.NewGuid().ToString(),
            SessionId = request.SessionId,
            Message = $"Generated quote based on your request: {request.Message}",
            EstimatedPrice = Random.Shared.Next(500, 5000),
            Currency = "USD",
            GeneratedAt = DateTime.UtcNow,
            Items = new List<QuoteItem>
            {
                new QuoteItem
                {
                    ProductName = "Auto Insurance",
                    Coverage = "Comprehensive",
                    Price = Random.Shared.Next(200, 1000),
                    Description = "Full coverage for your vehicle"
                },
                new QuoteItem
                {
                    ProductName = "Liability Insurance",
                    Coverage = "Standard",
                    Price = Random.Shared.Next(100, 500),
                    Description = "Third-party liability coverage"
                }
            }
        };

        _logger.LogInformation("Quote generated successfully: {QuoteId}", response.QuoteId);

        return Ok(response);
    }

    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { status = "Healthy", service = "QuoteBot", timestamp = DateTime.UtcNow });
    }
}
