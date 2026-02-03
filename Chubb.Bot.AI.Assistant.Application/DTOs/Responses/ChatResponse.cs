using Chubb.Bot.AI.Assistant.Application.DTOs.Common;

namespace Chubb.Bot.AI.Assistant.Application.DTOs.Responses;

public class ChatResponse
{
    /// <summary>
    /// ID de sesión
    /// </summary>
    public required string SessionId { get; set; }

    /// <summary>
    /// ID del bot que procesó la request
    /// </summary>
    public required string BotId { get; set; }

    /// <summary>
    /// Respuesta generada por el asistente
    /// </summary>
    public required string Response { get; set; }

    /// <summary>
    /// Indica si la cotización está completa (todas las 6 entidades recolectadas)
    /// </summary>
    public bool IsComplete { get; set; }

    /// <summary>
    /// Entidades recolectadas hasta el momento (opcional para debug/UI)
    /// </summary>
    public QuoteProgressInfo? Progress { get; set; }

    /// <summary>
    /// Timestamp de la respuesta
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
