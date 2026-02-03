using System.ComponentModel.DataAnnotations;

namespace Chubb.Bot.AI.Assistant.Application.DTOs.Requests;

public class FAQRequest
{
    [Required]
    [MaxLength(100)]
    public required string SessionId { get; set; }

    [Required]
    [MaxLength(50)]
    public required string BotId { get; set; }

    [Required]
    [MaxLength(2000)]
    public required string Message { get; set; }

    /// <summary>
    /// Categoría opcional para filtrar búsqueda de documentos
    /// </summary>
    [MaxLength(100)]
    public string? Category { get; set; }

    /// <summary>
    /// Número de documentos similares a recuperar (default: 7)
    /// </summary>
    public int TopK { get; set; } = 7;
}
