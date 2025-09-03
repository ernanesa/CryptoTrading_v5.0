using Dados.Data;
using Dados.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Dados.Tests;

public class TradeEntityTests : IDisposable
{
    private readonly CryptoTradingDbContext _context;

    public TradeEntityTests()
    {
        var options = new DbContextOptionsBuilder<CryptoTradingDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new CryptoTradingDbContext(options);
    }

    [Fact]
    public async Task TradeEntity_ShouldSaveAndRetrieve_Successfully()
    {
        // Arrange
        var trade = new TradeEntity
        {
            Tid = 12345,
            Symbol = "BTC-BRL",
            Date = DateTimeOffset.Now.ToUnixTimeSeconds(),
            Price = 275000.50m,
            Type = "buy",
            Amount = 0.001m,
            CollectedAt = DateTime.UtcNow
        };

        // Act
        await _context.Trades.AddAsync(trade);
        await _context.SaveChangesAsync();

        var retrievedTrade = await _context.Trades
            .FirstOrDefaultAsync(t => t.Tid == 12345);

        // Assert
        Assert.NotNull(retrievedTrade);
        Assert.Equal("BTC-BRL", retrievedTrade.Symbol);
        Assert.Equal(275000.50m, retrievedTrade.Price);
        Assert.Equal("buy", retrievedTrade.Type);
        Assert.Equal(0.001m, retrievedTrade.Amount);
    }

    [Fact]
    public async Task TradeEntity_ShouldHandleMultipleTradesForSameSymbol()
    {
        // Arrange
        var trades = new[]
        {
            new TradeEntity
            {
                Tid = 1001,
                Symbol = "BTC-BRL",
                Date = DateTimeOffset.Now.ToUnixTimeSeconds(),
                Price = 275000m,
                Type = "buy",
                Amount = 0.001m
            },
            new TradeEntity
            {
                Tid = 1002,
                Symbol = "BTC-BRL",
                Date = DateTimeOffset.Now.ToUnixTimeSeconds() + 1,
                Price = 275100m,
                Type = "sell",
                Amount = 0.0015m
            }
        };

        // Act
        await _context.Trades.AddRangeAsync(trades);
        await _context.SaveChangesAsync();

        var retrievedTrades = await _context.Trades
            .Where(t => t.Symbol == "BTC-BRL")
            .OrderBy(t => t.Tid)
            .ToListAsync();

        // Assert
        Assert.Equal(2, retrievedTrades.Count);
        Assert.Equal(1001, retrievedTrades[0].Tid);
        Assert.Equal(1002, retrievedTrades[1].Tid);
        Assert.Equal("buy", retrievedTrades[0].Type);
        Assert.Equal("sell", retrievedTrades[1].Type);
    }

    [Fact]
    public async Task TradeEntity_ShouldEnforceUniqueConstraintOnTid()
    {
        // Arrange
        var trade1 = new TradeEntity
        {
            Tid = 5000,
            Symbol = "BTC-BRL",
            Date = DateTimeOffset.Now.ToUnixTimeSeconds(),
            Price = 275000m,
            Type = "buy",
            Amount = 0.001m
        };

        var trade2 = new TradeEntity
        {
            Tid = 5000, // Same Tid
            Symbol = "ETH-BRL", // Different symbol
            Date = DateTimeOffset.Now.ToUnixTimeSeconds(),
            Price = 15000m,
            Type = "sell",
            Amount = 0.01m
        };

        // Act & Assert
        await _context.Trades.AddAsync(trade1);
        await _context.SaveChangesAsync();

        await _context.Trades.AddAsync(trade2);
        
        // Should throw exception due to duplicate primary key
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _context.SaveChangesAsync());
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}