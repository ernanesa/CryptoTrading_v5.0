namespace Dados.Entities;

public class TradeEntity
{
    // Tid não é único globalmente; chave real = (Symbol,Tid) configurada no DbContext
    public int Tid { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public long Date { get; set; }
    public decimal Price { get; set; }
    public string Type { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime CollectedAt { get; set; } = DateTime.UtcNow;
}
