namespace Chubb.Bot.AI.Assistant.Infrastructure.HttpClients.Interfaces;

public interface IQuoteBotClient
{
    Task<string> GetQuoteAsync(string query, CancellationToken cancellationToken = default);
    Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default);
}
