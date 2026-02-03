// COMENTADO TEMPORALMENTE - REQUIERE REDIS
/*
using Chubb.Bot.AI.Assistant.Application.DTOs.Common;
using Chubb.Bot.AI.Assistant.Core.Models;
using Chubb.Bot.AI.Assistant.Infrastructure.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Chubb.Bot.AI.Assistant.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SessionController : ControllerBase
{
    private readonly ISessionService _sessionService;
    private readonly ILogger<SessionController> _logger;

    public SessionController(ISessionService sessionService, ILogger<SessionController> logger)
    {
        _sessionService = sessionService;
        _logger = logger;
    }

    /// <summary>
    /// Crea una nueva sesión para un usuario
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(Session), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Session>> CreateSession(
        [FromBody] CreateSessionRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating session for user: {UserId}", request.UserId);

        var session = await _sessionService.CreateSessionAsync(
            request.UserId,
            request.Metadata,
            cancellationToken);

        return CreatedAtAction(nameof(GetSession), new { sessionId = session.SessionId }, session);
    }

    /// <summary>
    /// Obtiene información de una sesión
    /// </summary>
    [HttpGet("{sessionId}")]
    [ProducesResponseType(typeof(Session), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Session>> GetSession(
        string sessionId,
        CancellationToken cancellationToken)
    {
        var session = await _sessionService.GetSessionAsync(sessionId, cancellationToken);

        if (session == null)
        {
            return NotFound(new ErrorResponse
            {
                ErrorCode = "SESSION_NOT_FOUND",
                Message = $"Session {sessionId} not found or expired"
            });
        }

        return Ok(session);
    }

    /// <summary>
    /// Valida si una sesión está activa
    /// </summary>
    [HttpGet("{sessionId}/validate")]
    [ProducesResponseType(typeof(SessionValidationResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<SessionValidationResponse>> ValidateSession(
        string sessionId,
        CancellationToken cancellationToken)
    {
        var isValid = await _sessionService.ValidateSessionAsync(sessionId, cancellationToken);

        return Ok(new SessionValidationResponse(sessionId, isValid));
    }

    /// <summary>
    /// Extiende el tiempo de vida de una sesión
    /// </summary>
    [HttpPost("{sessionId}/extend")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExtendSession(
        string sessionId,
        [FromBody] ExtendSessionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _sessionService.ExtendSessionAsync(sessionId, request.AdditionalMinutes, cancellationToken);
            return Ok(new { message = "Session extended successfully" });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new ErrorResponse
            {
                ErrorCode = "SESSION_NOT_FOUND",
                Message = ex.Message
            });
        }
    }

    /// <summary>
    /// Obtiene todas las sesiones activas de un usuario
    /// </summary>
    [HttpGet("user/{userId}")]
    [ProducesResponseType(typeof(IEnumerable<Session>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<Session>>> GetUserSessions(
        string userId,
        CancellationToken cancellationToken)
    {
        var sessions = await _sessionService.GetUserSessionsAsync(userId, cancellationToken);
        return Ok(sessions);
    }

    /// <summary>
    /// Elimina una sesión
    /// </summary>
    [HttpDelete("{sessionId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSession(
        string sessionId,
        CancellationToken cancellationToken)
    {
        var deleted = await _sessionService.DeleteSessionAsync(sessionId, cancellationToken);

        if (!deleted)
        {
            return NotFound(new ErrorResponse
            {
                ErrorCode = "SESSION_NOT_FOUND",
                Message = $"Session {sessionId} not found"
            });
        }

        return NoContent();
    }
}

public record CreateSessionRequest(string UserId, Dictionary<string, string>? Metadata);
public record ExtendSessionRequest(int AdditionalMinutes);
public record SessionValidationResponse(string SessionId, bool IsValid);
*/
