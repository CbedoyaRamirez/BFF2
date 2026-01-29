using System.Text.Json;
using Chubb.Bot.AI.Assistant.Application.Interfaces;
using Chubb.Bot.AI.Assistant.Core.Constants;
using Chubb.Bot.AI.Assistant.Core.Exceptions;
using Chubb.Bot.AI.Assistant.Core.Models;
using Microsoft.Extensions.Caching.Distributed;

namespace Chubb.Bot.AI.Assistant.Application.Services;

public class SessionService : ISessionService
{
    private readonly IDistributedCache _cache;
    private readonly int _defaultTTLMinutes = 30;

    public SessionService(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task<Session> CreateSessionAsync(string userId, CancellationToken cancellationToken = default)
    {
        var session = new Session
        {
            SessionId = Guid.NewGuid().ToString(),
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            LastAccessedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_defaultTTLMinutes),
            Status = "Active"
        };

        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_defaultTTLMinutes)
        };

        var key = CacheKeys.GetSessionKey(session.SessionId);
        var serialized = JsonSerializer.Serialize(session);
        await _cache.SetStringAsync(key, serialized, options, cancellationToken);

        return session;
    }

    public async Task<Session?> GetSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        var key = CacheKeys.GetSessionKey(sessionId);
        var cached = await _cache.GetStringAsync(key, cancellationToken);

        if (string.IsNullOrEmpty(cached))
        {
            return null;
        }

        var session = JsonSerializer.Deserialize<Session>(cached);
        if (session != null)
        {
            session.LastAccessedAt = DateTime.UtcNow;
            await UpdateSessionAsync(session, cancellationToken);
        }

        return session;
    }

    public async Task<bool> DeleteSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        var key = CacheKeys.GetSessionKey(sessionId);
        await _cache.RemoveAsync(key, cancellationToken);
        return true;
    }

    public async Task<bool> ExtendSessionAsync(string sessionId, int minutesToAdd = 30, CancellationToken cancellationToken = default)
    {
        var session = await GetSessionAsync(sessionId, cancellationToken);
        if (session == null)
        {
            return false;
        }

        session.ExpiresAt = DateTime.UtcNow.AddMinutes(minutesToAdd);
        await UpdateSessionAsync(session, cancellationToken);
        return true;
    }

    private async Task UpdateSessionAsync(Session session, CancellationToken cancellationToken)
    {
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_defaultTTLMinutes)
        };

        var key = CacheKeys.GetSessionKey(session.SessionId);
        var serialized = JsonSerializer.Serialize(session);
        await _cache.SetStringAsync(key, serialized, options, cancellationToken);
    }
}
