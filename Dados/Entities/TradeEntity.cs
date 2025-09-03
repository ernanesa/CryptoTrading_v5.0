using System.ComponentModel.DataAnnotations;

namespace Dados.Entities;

public class TradeEntity
{
    [Key]
    public int Tid { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public long Date { get; set; }
    public decimal Price { get; set; }
    public string Type { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime CollectedAt { get; set; } = DateTime.UtcNow;
}
