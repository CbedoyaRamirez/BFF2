using Chubb.Bot.AI.Assistant.Application.DTOs.Common;
using Chubb.Bot.AI.Assistant.Application.DTOs.Requests;
using Chubb.Bot.AI.Assistant.Application.DTOs.Responses;
using Chubb.Bot.AI.Assistant.Infrastructure.HttpClients.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Chubb.Bot.AI.Assistant.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FAQController : ControllerBase
{
    private readonly IFAQBotClient _faqBotClient;
    private readonly ILogger<FAQController> _logger;

    public FAQController(IFAQBotClient faqBotClient, ILogger<FAQController> logger)
    {
        _faqBotClient = faqBotClient;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene una respuesta a una pregunta frecuente
    /// </summary>
    /// <param name="request">Pregunta del usuario</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Respuesta a la pregunta</returns>
    [HttpPost]
    [ProducesResponseType(typeof(FAQResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<FAQResponse>> GetAnswer(
        [FromBody] FAQRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Getting FAQ answer for bot {BotId}, session {SessionId}, category: {Category}",
            request.BotId,
            request.SessionId,
            request.Category ?? "None");

        var response = await _faqBotClient.GetAnswerAsync(request, cancellationToken);

        _logger.LogInformation(
            "FAQ response received. Retrieved {ChunkCount} chunks",
            response.RetrievedChunks);

        return Ok(response);
    }
}
