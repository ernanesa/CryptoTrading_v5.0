using Dados.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dados.Data;

public class CryptoTradingDbContext : DbContext
{
    public CryptoTradingDbContext(DbContextOptions<CryptoTradingDbContext> options)
        : base(options)
    {
    }

    public DbSet<SymbolEntity> Symbols { get; set; }
    public DbSet<TickerEntity> Tickers { get; set; }
    public DbSet<OrderBookEntity> OrderBooks { get; set; }
    public DbSet<TradeEntity> Trades { get; set; }
    public DbSet<CandleEntity> Candles { get; set; }
    public DbSet<AssetFeeEntity> AssetFees { get; set; }
    public DbSet<AssetNetworkEntity> AssetNetworks { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // SymbolEntity
        modelBuilder.Entity<SymbolEntity>()
            .HasKey(s => s.Symbol);

        // TickerEntity
        modelBuilder.Entity<TickerEntity>()
            .HasKey(t => t.Symbol);

        // OrderBookEntity
        modelBuilder.Entity<OrderBookEntity>()
            .Property(ob => ob.Bids)
            .HasColumnType("text");
        modelBuilder.Entity<OrderBookEntity>()
            .Property(ob => ob.Asks)
            .HasColumnType("text");

        // TradeEntity
        modelBuilder.Entity<TradeEntity>()
            .HasKey(t => new { t.Symbol, t.Tid });

        // CandleEntity
        modelBuilder.Entity<CandleEntity>()
            .HasIndex(c => new { c.Symbol, c.Resolution, c.Timestamp });

        // AssetFeeEntity
        modelBuilder.Entity<AssetFeeEntity>()
            .HasKey(af => af.Asset);

        // AssetNetworkEntity
        modelBuilder.Entity<AssetNetworkEntity>()
            .HasIndex(an => new { an.Asset, an.Network });
    }
}
