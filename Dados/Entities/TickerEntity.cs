using System.ComponentModel.DataAnnotations;

namespace Dados.Entities;

public class TickerEntity
{
    [Key]
    public string Symbol { get; set; } = string.Empty;
    public decimal Last { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Vol { get; set; }
    public decimal Buy { get; set; }
    public decimal Sell { get; set; }
    public long Date { get; set; }
    public DateTime CollectedAt { get; set; } = DateTime.UtcNow;
}
