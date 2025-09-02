using System.ComponentModel.DataAnnotations;

namespace Dados.Entities;

public class AssetFeeEntity
{
    [Key]
    public string Asset { get; set; } = string.Empty;
    public decimal WithdrawalFee { get; set; }
    public DateTime CollectedAt { get; set; } = DateTime.UtcNow;
}
