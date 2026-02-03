using Chubb.Bot.AI.Assistant.Application.DTOs.Requests;
using Chubb.Bot.AI.Assistant.Application.DTOs.Responses;
using Chubb.Bot.AI.Assistant.Infrastructure.HttpClients.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Chubb.Bot.AI.Assistant.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QuoteController : ControllerBase
{
    private readonly IQuoteBotClient _quoteBotClient;
    private readonly ILogger<QuoteController> _logger;

    public QuoteController(IQuoteBotClient quoteBotClient, ILogger<QuoteController> logger)
    {
        _quoteBotClient = quoteBotClient;
        _logger = logger;
    }

    /// <summary>
    /// Genera una cotización basada en la consulta del usuario
    /// </summary>
    /// <param name="request">Solicitud de cotización</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Cotización generada</returns>
    [HttpPost("generate")]
    [ProducesResponseType(typeof(QuoteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<QuoteResponse>> GenerateQuote(
        [FromBody] QuoteRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Generating quote for query: {Query}", request.Query);

        var quote = await _quoteBotClient.GetQuoteAsync(request.Query, cancellationToken);

        var response = new QuoteResponse
        {
            Quote = quote,
            SessionId = request.SessionId,
            Timestamp = DateTime.UtcNow
        };

        return Ok(response);
    }
}
