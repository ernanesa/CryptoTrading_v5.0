using System.ComponentModel.DataAnnotations;

namespace Dados.Entities;

public class AssetNetworkEntity
{
    [Key]
    public int Id { get; set; }
    public string Asset { get; set; } = string.Empty;
    public string Network { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public decimal WithdrawalFee { get; set; }
    public decimal MinWithdrawalAmount { get; set; }
    public bool WithdrawalEnabled { get; set; }
    public DateTime CollectedAt { get; set; } = DateTime.UtcNow;
}
