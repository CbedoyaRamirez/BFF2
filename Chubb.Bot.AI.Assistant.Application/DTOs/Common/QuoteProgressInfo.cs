namespace Chubb.Bot.AI.Assistant.Application.DTOs.Common;

public class QuoteProgressInfo
{
    /// <summary>
    /// Número de entidades recolectadas
    /// </summary>
    public int CollectedEntities { get; set; }

    /// <summary>
    /// Total de entidades requeridas
    /// </summary>
    public int TotalEntities { get; set; } = 6;

    /// <summary>
    /// Porcentaje de progreso (0-100)
    /// </summary>
    public int ProgressPercentage => TotalEntities > 0 ? (CollectedEntities * 100) / TotalEntities : 0;

    /// <summary>
    /// Entidades faltantes
    /// </summary>
    public List<string> MissingEntities { get; set; } = new();

    /// <summary>
    /// Entidades ya recolectadas con sus valores
    /// </summary>
    public Dictionary<string, string> CollectedData { get; set; } = new();
}
