using System.ComponentModel.DataAnnotations;

namespace Dados.Entities;

public class CandleEntity
{
    [Key]
    public int Id { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string Resolution { get; set; } = string.Empty;
    public long Timestamp { get; set; }
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public decimal Volume { get; set; }
    public DateTime CollectedAt { get; set; } = DateTime.UtcNow;
}
