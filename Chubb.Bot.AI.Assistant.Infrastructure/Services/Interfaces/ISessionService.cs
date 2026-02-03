using Chubb.Bot.AI.Assistant.Core.Models;

namespace Chubb.Bot.AI.Assistant.Infrastructure.Services.Interfaces;

public interface ISessionService
{
    Task<Session?> GetSessionAsync(string sessionId, CancellationToken cancellationToken = default);
    Task<Session> CreateSessionAsync(string userId, Dictionary<string, string>? metadata = null, CancellationToken cancellationToken = default);
    Task UpdateSessionAsync(Session session, CancellationToken cancellationToken = default);
    Task<bool> DeleteSessionAsync(string sessionId, CancellationToken cancellationToken = default);
    Task<bool> ValidateSessionAsync(string sessionId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Session>> GetUserSessionsAsync(string userId, CancellationToken cancellationToken = default);
    Task ExtendSessionAsync(string sessionId, int additionalMinutes, CancellationToken cancellationToken = default);
}
