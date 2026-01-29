using Chubb.Bot.AI.Assistant.Application.DTOs.Requests;
using Chubb.Bot.AI.Assistant.Application.DTOs.Responses;
using Chubb.Bot.AI.Assistant.Application.Interfaces;
using Chubb.Bot.AI.Assistant.Core.Exceptions;
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

    [HttpPost]
    public async Task<ActionResult<SessionResponse>> CreateSession([FromBody] SessionRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating session for user {UserId}", request.UserId);

        var session = await _sessionService.CreateSessionAsync(request.UserId, cancellationToken);

        var response = new SessionResponse
        {
            SessionId = session.SessionId,
            UserId = session.UserId,
            CreatedAt = session.CreatedAt,
            ExpiresAt = session.ExpiresAt,
            Status = session.Status
        };

        return CreatedAtAction(nameof(GetSession), new { sessionId = session.SessionId }, response);
    }

    [HttpGet("{sessionId}")]
    public async Task<ActionResult<SessionResponse>> GetSession(string sessionId, CancellationToken cancellationToken)
    {
        var session = await _sessionService.GetSessionAsync(sessionId, cancellationToken);

        if (session == null)
        {
            throw new NotFoundException("Session", sessionId);
        }

        var response = new SessionResponse
        {
            SessionId = session.SessionId,
            UserId = session.UserId,
            CreatedAt = session.CreatedAt,
            ExpiresAt = session.ExpiresAt,
            Status = session.Status
        };

        return Ok(response);
    }

    [HttpDelete("{sessionId}")]
    public async Task<ActionResult> DeleteSession(string sessionId, CancellationToken cancellationToken)
    {
        var result = await _sessionService.DeleteSessionAsync(sessionId, cancellationToken);
        return NoContent();
    }
}
