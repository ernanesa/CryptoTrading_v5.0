using System.ComponentModel.DataAnnotations;

namespace Dados.Entities;

public class SymbolEntity
{
    [Key]
    public string Symbol { get; set; } = string.Empty;
    public string BaseCurrency { get; set; } = string.Empty;
    public string QuoteCurrency { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int BasePrecision { get; set; }
    public int QuotePrecision { get; set; }
    public int AmountPrecision { get; set; }
    public decimal MinOrderAmount { get; set; }
    public decimal MinOrderValue { get; set; }
    public DateTime CollectedAt { get; set; } = DateTime.UtcNow;
}
