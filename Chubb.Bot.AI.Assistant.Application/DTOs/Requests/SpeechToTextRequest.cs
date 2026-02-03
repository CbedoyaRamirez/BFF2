using System.ComponentModel.DataAnnotations;

namespace Chubb.Bot.AI.Assistant.Application.DTOs.Requests;

public class SpeechToTextRequest
{
    [Required]
    public required string AudioBase64 { get; set; }

    [MaxLength(10)]
    [Required]
    public required string AudioFormat { get; set; }

    [MaxLength(10)]
    public string Language { get; set; } = "es-ES";

    public int? SampleRate { get; set; }
}
