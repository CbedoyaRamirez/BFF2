using System.ComponentModel.DataAnnotations;

namespace Chubb.Bot.AI.Assistant.Application.DTOs.Requests;

public class ChatRequest
{
    /// <summary>
    /// ID de sesión del usuario (genera uno nuevo si no existe)
    /// </summary>
    [Required]
    [MaxLength(100)]
    public required string SessionId { get; set; }

    /// <summary>
    /// ID del bot a invocar (ej: "quote-auto")
    /// </summary>
    [Required]
    [MaxLength(50)]
    public required string BotId { get; set; }

    /// <summary>
    /// Mensaje del usuario
    /// </summary>
    [Required]
    [MaxLength(2000)]
    public required string Message { get; set; }

    /// <summary>
    /// Metadata adicional (opcional)
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }
}
