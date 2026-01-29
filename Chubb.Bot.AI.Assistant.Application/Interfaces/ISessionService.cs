using Chubb.Bot.AI.Assistant.Core.Models;

namespace Chubb.Bot.AI.Assistant.Application.Interfaces;

public interface ISessionService
{
    Task<Session> CreateSessionAsync(string userId, CancellationToken cancellationToken = default);
    Task<Session?> GetSessionAsync(string sessionId, CancellationToken cancellationToken = default);
    Task<bool> DeleteSessionAsync(string sessionId, CancellationToken cancellationToken = default);
    Task<bool> ExtendSessionAsync(string sessionId, int minutesToAdd = 30, CancellationToken cancellationToken = default);
}
