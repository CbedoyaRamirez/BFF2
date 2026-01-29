namespace Chubb.Bot.AI.Assistant.Infrastructure.HttpClients.Interfaces;

public interface IFAQBotClient
{
    Task<string> GetAnswerAsync(string question, CancellationToken cancellationToken = default);
    Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default);
}
