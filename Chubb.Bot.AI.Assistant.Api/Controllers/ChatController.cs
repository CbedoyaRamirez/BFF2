using Chubb.Bot.AI.Assistant.Application.DTOs.Common;
using Chubb.Bot.AI.Assistant.Application.DTOs.Requests;
using Chubb.Bot.AI.Assistant.Application.DTOs.Responses;
using Chubb.Bot.AI.Assistant.Infrastructure.HttpClients.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Chubb.Bot.AI.Assistant.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IChatBotClient _chatBotClient;
    private readonly ILogger<ChatController> _logger;

    public ChatController(IChatBotClient chatBotClient, ILogger<ChatController> logger)
    {
        _chatBotClient = chatBotClient;
        _logger = logger;
    }

    /// <summary>
    /// Envía un mensaje al bot de chat y obtiene una respuesta
    /// </summary>
    /// <param name="request">Solicitud de chat</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Respuesta del bot</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ChatResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<ChatResponse>> SendMessage(
        [FromBody] ChatRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Sending message to bot {BotId} for session {SessionId}",
            request.BotId,
            request.SessionId);

        var response = await _chatBotClient.SendMessageAsync(request, cancellationToken);

        _logger.LogInformation(
            "Received response from bot {BotId}. IsComplete: {IsComplete}",
            response.BotId,
            response.IsComplete);

        return Ok(response);
    }
}
