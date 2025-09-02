using System.ComponentModel.DataAnnotations;

namespace Dados.Entities;

public class OrderBookEntity
{
    [Key]
    public int Id { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string Bids { get; set; } = "[]"; // JSON string for bids array
    public string Asks { get; set; } = "[]"; // JSON string for asks array
    public DateTime CollectedAt { get; set; } = DateTime.UtcNow;
}
