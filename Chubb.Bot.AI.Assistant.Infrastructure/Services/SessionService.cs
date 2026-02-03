using Chubb.Bot.AI.Assistant.Core.Enums;
using Chubb.Bot.AI.Assistant.Core.Models;
using Chubb.Bot.AI.Assistant.Infrastructure.Redis;
using Chubb.Bot.AI.Assistant.Infrastructure.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;

namespace Chubb.Bot.AI.Assistant.Infrastructure.Services;

public class SessionService : ISessionService
{
    private readonly IDatabase _redisDb;
    private readonly ILogger<SessionService> _logger;
    private readonly int _defaultTtlMinutes;
    private const string SessionPrefix = "session:";
    private const string UserSessionsPrefix = "user:sessions:";

    public SessionService(IConfiguration configuration, ILogger<SessionService> logger)
    {
        _redisDb = RedisConnectionFactory.GetDatabase();
        _logger = logger;
        _defaultTtlMinutes = configuration.GetValue<int>("RedisSettings:DefaultTTLMinutes", 30);
    }

    public async Task<Session?> GetSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = $"{SessionPrefix}{sessionId}";
            var sessionData = await _redisDb.StringGetAsync(key);

            if (sessionData.IsNullOrEmpty)
            {
                _logger.LogWarning("Session not found: {SessionId}", sessionId);
                return null;
            }

            var session = JsonSerializer.Deserialize<Session>(sessionData!);

            // Verificar si la sesión ha expirado
            if (session != null && session.ExpiresAt < DateTime.UtcNow)
            {
                _logger.LogWarning("Session expired: {SessionId}", sessionId);
                session.Status = SessionStatus.Expired.ToString();
                await UpdateSessionAsync(session, cancellationToken);
                return null;
            }

            // Actualizar LastAccessedAt
            if (session != null)
            {
                session.LastAccessedAt = DateTime.UtcNow;
                await UpdateSessionAsync(session, cancellationToken);
            }

            return session;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting session: {SessionId}", sessionId);
            throw;
        }
    }

    public async Task<Session> CreateSessionAsync(string userId, Dictionary<string, string>? metadata = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var sessionId = Guid.NewGuid().ToString();
            var now = DateTime.UtcNow;

            var session = new Session
            {
                SessionId = sessionId,
                UserId = userId,
                CreatedAt = now,
                LastAccessedAt = now,
                ExpiresAt = now.AddMinutes(_defaultTtlMinutes),
                Metadata = metadata ?? new Dictionary<string, string>(),
                Status = SessionStatus.Active.ToString()
            };

            var key = $"{SessionPrefix}{sessionId}";
            var sessionData = JsonSerializer.Serialize(session);

            await _redisDb.StringSetAsync(key, sessionData, TimeSpan.FromMinutes(_defaultTtlMinutes));

            // Agregar a la lista de sesiones del usuario
            var userSessionsKey = $"{UserSessionsPrefix}{userId}";
            await _redisDb.SetAddAsync(userSessionsKey, sessionId);

            _logger.LogInformation("Session created: {SessionId} for user: {UserId}", sessionId, userId);

            return session;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating session for user: {UserId}", userId);
            throw;
        }
    }

    public async Task UpdateSessionAsync(Session session, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = $"{SessionPrefix}{session.SessionId}";
            var sessionData = JsonSerializer.Serialize(session);

            var ttl = session.ExpiresAt - DateTime.UtcNow;
            if (ttl.TotalMinutes > 0)
            {
                await _redisDb.StringSetAsync(key, sessionData, ttl);
                _logger.LogDebug("Session updated: {SessionId}", session.SessionId);
            }
            else
            {
                _logger.LogWarning("Cannot update expired session: {SessionId}", session.SessionId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating session: {SessionId}", session.SessionId);
            throw;
        }
    }

    public async Task<bool> DeleteSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Obtener la sesión para encontrar el userId
            var session = await GetSessionAsync(sessionId, cancellationToken);
            if (session == null)
            {
                return false;
            }

            var key = $"{SessionPrefix}{sessionId}";
            var deleted = await _redisDb.KeyDeleteAsync(key);

            // Remover de la lista de sesiones del usuario
            var userSessionsKey = $"{UserSessionsPrefix}{session.UserId}";
            await _redisDb.SetRemoveAsync(userSessionsKey, sessionId);

            _logger.LogInformation("Session deleted: {SessionId}", sessionId);

            return deleted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting session: {SessionId}", sessionId);
            throw;
        }
    }

    public async Task<bool> ValidateSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        var session = await GetSessionAsync(sessionId, cancellationToken);
        return session != null && session.Status == SessionStatus.Active.ToString();
    }

    public async Task<IEnumerable<Session>> GetUserSessionsAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var userSessionsKey = $"{UserSessionsPrefix}{userId}";
            var sessionIds = await _redisDb.SetMembersAsync(userSessionsKey);

            var sessions = new List<Session>();

            foreach (var sessionId in sessionIds)
            {
                var session = await GetSessionAsync(sessionId!, cancellationToken);
                if (session != null)
                {
                    sessions.Add(session);
                }
            }

            return sessions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sessions for user: {UserId}", userId);
            throw;
        }
    }

    public async Task ExtendSessionAsync(string sessionId, int additionalMinutes, CancellationToken cancellationToken = default)
    {
        try
        {
            var session = await GetSessionAsync(sessionId, cancellationToken);
            if (session == null)
            {
                throw new InvalidOperationException($"Session not found: {sessionId}");
            }

            session.ExpiresAt = session.ExpiresAt.AddMinutes(additionalMinutes);
            await UpdateSessionAsync(session, cancellationToken);

            _logger.LogInformation("Session extended: {SessionId} by {Minutes} minutes", sessionId, additionalMinutes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extending session: {SessionId}", sessionId);
            throw;
        }
    }
}
