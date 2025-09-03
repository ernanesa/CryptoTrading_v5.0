using Dados.Controllers;
using Dados.Data;
using Dados.Services;
using MercadoBitcoin.Client;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Dados.Tests;

public class CollectTradesTests : IDisposable
{
    private readonly CryptoTradingDbContext _context;
    private readonly Mock<ILogger<DataController>> _mockControllerLogger;
    private readonly Mock<ILogger<DataIngestionService>> _mockServiceLogger;
    private readonly Mock<MercadoBitcoinClient> _mockClient;
    private readonly DataIngestionService _service;
    private readonly DataController _controller;

    public CollectTradesTests()
    {
        var options = new DbContextOptionsBuilder<CryptoTradingDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new CryptoTradingDbContext(options);
        _mockControllerLogger = new Mock<ILogger<DataController>>();
        _mockServiceLogger = new Mock<ILogger<DataIngestionService>>();
        _mockClient = new Mock<MercadoBitcoinClient>();

        _service = new DataIngestionService(_context, _mockClient.Object, _mockServiceLogger.Object);
        _controller = new DataController(_service, _context, _mockClient.Object, _mockControllerLogger.Object);
    }

    [Fact]
    public async Task CollectTrades_ShouldReturnOk_WhenSuccessful()
    {
        // Arrange
        await SeedTestData();

        // Act
        var result = await _controller.CollectTrades();

        // Assert
        Assert.IsType<OkObjectResult>(result);
        var okResult = result as OkObjectResult;
        Assert.Equal("Trades collected successfully for all active symbols", okResult?.Value);
    }

    [Fact]
    public async Task CollectTrades_ShouldReturnInternalServerError_WhenExceptionOccurs()
    {
        // Arrange
        _mockClient.Setup(x => x.GetTradesAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<int?>(), 
            It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<int?>()))
            .ThrowsAsync(new Exception("Test exception"));

        await SeedTestData();

        // Act
        var result = await _controller.CollectTrades();

        // Assert
        Assert.IsType<ObjectResult>(result);
        var objectResult = result as ObjectResult;
        Assert.Equal(500, objectResult?.StatusCode);
        Assert.Equal("Error collecting trades", objectResult?.Value);
    }

    [Fact]
    public async Task CollectTrades_WithLimit_ShouldPassLimitToService()
    {
        // Arrange
        await SeedTestData();
        const int testLimit = 50;

        // Act
        var result = await _controller.CollectTrades(testLimit);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        
        // Verify that the limit was used in the API call
        _mockClient.Verify(x => x.GetTradesAsync(It.IsAny<string>(), 
            It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<int?>(), testLimit), 
            Times.AtLeast(1));
    }

    private async Task SeedTestData()
    {
        // Add test symbols
        var symbols = new[]
        {
            new Dados.Entities.SymbolEntity
            {
                Symbol = "BTC-BRL",
                BaseCurrency = "BTC",
                QuoteCurrency = "BRL",
                Status = "ACTIVE",
                BasePrecision = 8,
                QuotePrecision = 2,
                AmountPrecision = 8,
                MinOrderAmount = 0.00000001m,
                MinOrderValue = 1.00m,
                CollectedAt = DateTime.UtcNow
            },
            new Dados.Entities.SymbolEntity
            {
                Symbol = "ETH-BRL",
                BaseCurrency = "ETH",
                QuoteCurrency = "BRL",
                Status = "ACTIVE",
                BasePrecision = 8,
                QuotePrecision = 2,
                AmountPrecision = 8,
                MinOrderAmount = 0.00000001m,
                MinOrderValue = 1.00m,
                CollectedAt = DateTime.UtcNow
            }
        };

        await _context.Symbols.AddRangeAsync(symbols);
        await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}