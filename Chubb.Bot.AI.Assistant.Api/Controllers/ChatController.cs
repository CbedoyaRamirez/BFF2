using Chubb.Bot.AI.Assistant.Api.Helpers;
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
        // Usa LogPerformance para medir el tiempo de la operación (se escribe en logs/performance/)
        using (var perfLogger = LoggingHelper.LogPerformance("ChatController.SendMessage"))
        {
            try
            {
                // Agrega contexto al log de performance
                perfLogger.AddContext("BotId", request.BotId);
                perfLogger.AddContext("SessionId", request.SessionId);

                // Log de desarrollo para debugging (se escribe en logs/dev/)
                LoggingHelper.LogDevelopment(
                    "Processing chat message for session {SessionId} with bot {BotId}",
                    request.SessionId,
                    request.BotId);

                _logger.LogInformation(
                    "Sending message to bot {BotId} for session {SessionId}",
                    request.BotId,
                    request.SessionId);

                var response = await _chatBotClient.SendMessageAsync(request, cancellationToken);

                // Agrega información de la respuesta al log de performance
                perfLogger.AddContext("ResponseReceived", true);
                perfLogger.AddContext("IsComplete", response.IsComplete);

                _logger.LogInformation(
                    "Received response from bot {BotId}. IsComplete: {IsComplete}",
                    response.BotId,
                    response.IsComplete);

                return Ok(response);
            }
            catch (HttpRequestException hex)
            {
                // Los errores se escriben automáticamente en logs/error/
                LoggingHelper.LogError(
                    "HTTP error calling ChatBot service for session {SessionId}",
                    hex,
                    request.SessionId);

                return StatusCode(
                    StatusCodes.Status503ServiceUnavailable,
                    new ErrorResponse
                    {
                        Message = "Chat service is temporarily unavailable",
                        ErrorCode = "SERVICE_UNAVAILABLE"
                    });
            }
            catch (Exception ex)
            {
                // Los errores se escriben automáticamente en logs/error/
                LoggingHelper.LogError(
                    "Unexpected error processing chat message for session {SessionId}",
                    ex,
                    request.SessionId);

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new ErrorResponse
                    {
                        Message = "An unexpected error occurred",
                        ErrorCode = "INTERNAL_ERROR"
                    });
            }
        }
    }
}
