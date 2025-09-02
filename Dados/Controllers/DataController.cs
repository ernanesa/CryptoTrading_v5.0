using Dados.Data;
using Dados.Services;
using MercadoBitcoin.Client;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Dados.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DataController : ControllerBase
{
    private readonly DataIngestionService _dataIngestionService;
    private readonly CryptoTradingDbContext _dbContext;
    private readonly MercadoBitcoinClient _mercadoBitcoinClient;
    private readonly ILogger<DataController> _logger;

    public DataController(
        DataIngestionService dataIngestionService,
        CryptoTradingDbContext dbContext,
        MercadoBitcoinClient mercadoBitcoinClient,
        ILogger<DataController> logger)
    {
        _dataIngestionService = dataIngestionService;
        _dbContext = dbContext;
        _mercadoBitcoinClient = mercadoBitcoinClient;
        _logger = logger;
    }

    /// <summary>
    /// Resumo com contagem de registros por tabela e últimos timestamps coletados
    /// </summary>
    /// <returns>Objeto com métricas de dados</returns>
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
    {
        try
        {
            var summary = new
            {
                Symbols = await _dbContext.Symbols.CountAsync(),
                Tickers = await _dbContext.Tickers.CountAsync(),
                OrderBooks = await _dbContext.OrderBooks.CountAsync(),
                Trades = await _dbContext.Trades.CountAsync(),
                Candles = await _dbContext.Candles.CountAsync(),
                AssetFees = await _dbContext.AssetFees.CountAsync(),
                AssetNetworks = await _dbContext.AssetNetworks.CountAsync(),
                LastTickerAt = await _dbContext.Tickers.MaxAsync(t => (DateTime?)t.CollectedAt),
                LastOrderBookAt = await _dbContext.OrderBooks.MaxAsync(t => (DateTime?)t.CollectedAt),
                LastTradeAt = await _dbContext.Trades.MaxAsync(t => (DateTime?)t.CollectedAt),
                LastCandleAt = await _dbContext.Candles.MaxAsync(t => (DateTime?)t.CollectedAt),
                LastAssetFeeAt = await _dbContext.AssetFees.MaxAsync(t => (DateTime?)t.CollectedAt),
                LastAssetNetworkAt = await _dbContext.AssetNetworks.MaxAsync(t => (DateTime?)t.CollectedAt)
            };
            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error building summary");
            return StatusCode(500, "Error building summary");
        }
    }

    /// <summary>
    /// Executa a coleta completa de todos os tipos de dados na ordem recomendada
    /// </summary>
    [HttpGet("collect-all")]
    public async Task<IActionResult> CollectAll(CancellationToken cancellationToken = default)
    {
        try
        {
            await _dataIngestionService.CollectAllAsync(cancellationToken);
            return Ok("Full ingestion completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in CollectAll");
            return StatusCode(500, "Error executing full ingestion");
        }
    }

    /// <summary>
    /// Coleta e persiste todos os símbolos disponíveis na API do Mercado Bitcoin
    /// </summary>
    /// <param name="cancellationToken">Token para cancelamento da operação</param>
    /// <returns>Resultado da operação de coleta de símbolos</returns>
    [HttpGet("collect-symbols")]
    public async Task<IActionResult> CollectSymbols(CancellationToken cancellationToken)
    {
        try
        {
            await _dataIngestionService.CollectSymbolsAsync(cancellationToken);
            return Ok("Symbols collected successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in CollectSymbols");
            return StatusCode(500, "Error collecting symbols");
        }
    }

    /// <summary>
    /// Coleta e persiste tickers (preços atuais) das 50 criptomoedas selecionadas
    /// </summary>
    /// <param name="cancellationToken">Token para cancelamento da operação</param>
    /// <returns>Resultado da operação de coleta de tickers</returns>
    [HttpGet("collect-tickers")]
    public async Task<IActionResult> CollectTickers(CancellationToken cancellationToken = default)
    {
        try
        {
            await _dataIngestionService.CollectTickersAsync(cancellationToken);
            return Ok("Tickers collected successfully for all active symbols");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in CollectTickers");
            return StatusCode(500, "Error collecting tickers");
        }
    }

    /// <summary>
    /// Coleta e persiste order books (livros de ofertas) das 50 criptomoedas selecionadas
    /// </summary>
    /// <param name="limit">Limite de ordens por lado do livro (padrão: 10)</param>
    /// <param name="cancellationToken">Token para cancelamento da operação</param>
    /// <returns>Resultado da operação de coleta de order books</returns>
    [HttpGet("collect-order-books")]
    public async Task<IActionResult> CollectOrderBooks([FromQuery] int? limit = 10, CancellationToken cancellationToken = default)
    {
        try
        {
            await _dataIngestionService.CollectOrderBookAsync(limit, cancellationToken);
            return Ok("Order books collected successfully for all active symbols");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in CollectOrderBooks");
            return StatusCode(500, "Error collecting order books");
        }
    }

    /// <summary>
    /// Coleta e persiste trades recentes das 50 criptomoedas selecionadas
    /// </summary>
    /// <param name="limit">Número máximo de trades por símbolo (opcional)</param>
    /// <param name="cancellationToken">Token para cancelamento da operação</param>
    /// <returns>Resultado da operação de coleta de trades</returns>
    [HttpGet("collect-trades")]
    public async Task<IActionResult> CollectTrades([FromQuery] int? limit = null, CancellationToken cancellationToken = default)
    {
        try
        {
            await _dataIngestionService.CollectTradesAsync(limit, cancellationToken);
            return Ok("Trades collected successfully for all active symbols");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in CollectTrades");
            return StatusCode(500, "Error collecting trades");
        }
    }

    /// <summary>
    /// Coleta e persiste candles (velas) das 50 criptomoedas selecionadas
    /// </summary>
    /// <param name="resolution">Resolução temporal dos candles (ex: 1m, 5m, 1h, 1d)</param>
    /// <param name="countback">Número de candles para coletar (opcional)</param>
    /// <param name="cancellationToken">Token para cancelamento da operação</param>
    /// <returns>Resultado da operação de coleta de candles</returns>
    [HttpGet("collect-candles")]
    public async Task<IActionResult> CollectCandles(
        [FromQuery] string resolution = "1h",
        [FromQuery] int? countback = 24,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _dataIngestionService.CollectCandlesAsync(resolution, countback, cancellationToken);
            return Ok($"Candles collected successfully for all active symbols with resolution {resolution}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in CollectCandles");
            return StatusCode(500, "Error collecting candles");
        }
    }

    /// <summary>
    /// Coleta e persiste taxas de ativos das 50 criptomoedas selecionadas
    /// </summary>
    /// <param name="cancellationToken">Token para cancelamento da operação</param>
    /// <returns>Resultado da operação de coleta de taxas de ativos</returns>
    [HttpGet("collect-asset-fees")]
    public async Task<IActionResult> CollectAssetFees(CancellationToken cancellationToken = default)
    {
        try
        {
            await _dataIngestionService.CollectAssetFeesAsync(cancellationToken);
            return Ok("Asset fees collected successfully for all active assets");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in CollectAssetFees");
            return StatusCode(500, "Error collecting asset fees");
        }
    }

    /// <summary>
    /// Coleta e persiste redes de ativos das 50 criptomoedas selecionadas
    /// </summary>
    /// <param name="cancellationToken">Token para cancelamento da operação</param>
    /// <returns>Resultado da operação de coleta de redes de ativos</returns>
    [HttpGet("collect-asset-networks")]
    public async Task<IActionResult> CollectAssetNetworks(CancellationToken cancellationToken = default)
    {
        try
        {
            await _dataIngestionService.CollectAssetNetworksAsync(cancellationToken);
            return Ok("Asset networks collected successfully for all active assets");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in CollectAssetNetworks");
            return StatusCode(500, "Error collecting asset networks");
        }
    }

    /// <summary>
    /// Verifica o status de saúde do serviço
    /// </summary>
    /// <returns>Status de saúde do serviço com timestamp</returns>
    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }

    /// <summary>
    /// Lista todos os símbolos coletados
    /// </summary>
    /// <returns>Lista de símbolos coletados</returns>
    [HttpGet("symbols")]
    public async Task<IActionResult> GetSymbols()
    {
        try
        {
            var symbols = await _dataIngestionService.GetSymbolsAsync();
            return Ok(symbols);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting symbols");
            return StatusCode(500, "Error getting symbols");
        }
    }

    /// <summary>
    /// Lista todos os tickers coletados
    /// </summary>
    /// <returns>Lista de tickers coletados</returns>
    [HttpGet("tickers")]
    public async Task<IActionResult> GetTickers()
    {
        try
        {
            var tickers = await _dbContext.Tickers
                .OrderBy(t => t.Symbol)
                .ToListAsync();
            return Ok(tickers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tickers");
            return StatusCode(500, "Error getting tickers");
        }
    }

    /// <summary>
    /// Lista todos os order books coletados
    /// </summary>
    /// <returns>Lista de order books coletados</returns>
    [HttpGet("order-books")]
    public async Task<IActionResult> GetOrderBooks()
    {
        try
        {
            var orderBooks = await _dbContext.OrderBooks
                .OrderBy(ob => ob.Symbol)
                .ToListAsync();
            return Ok(orderBooks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting order books");
            return StatusCode(500, "Error getting order books");
        }
    }

    /// <summary>
    /// Lista todos os trades coletados
    /// </summary>
    /// <returns>Lista de trades coletados</returns>
    [HttpGet("trades")]
    public async Task<IActionResult> GetTrades()
    {
        try
        {
            var trades = await _dbContext.Trades
                .OrderBy(t => t.Symbol)
                .ThenByDescending(t => t.Date)
                .Take(100) // Limitar para não sobrecarregar
                .ToListAsync();
            return Ok(trades);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting trades");
            return StatusCode(500, "Error getting trades");
        }
    }

    /// <summary>
    /// Lista todos os candles coletados
    /// </summary>
    /// <returns>Lista de candles coletados</returns>
    [HttpGet("candles")]
    public async Task<IActionResult> GetCandles()
    {
        try
        {
            var candles = await _dbContext.Candles
                .OrderBy(c => c.Symbol)
                .ThenBy(c => c.Timestamp)
                .Take(100) // Limitar para não sobrecarregar
                .ToListAsync();
            return Ok(candles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting candles");
            return StatusCode(500, "Error getting candles");
        }
    }

    /// <summary>
    /// Lista todas as taxas de ativos coletadas
    /// </summary>
    /// <returns>Lista de taxas de ativos coletadas</returns>
    [HttpGet("asset-fees")]
    public async Task<IActionResult> GetAssetFees()
    {
        try
        {
            var assetFees = await _dbContext.AssetFees
                .OrderBy(af => af.Asset)
                .ToListAsync();
            return Ok(assetFees);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting asset fees");
            return StatusCode(500, "Error getting asset fees");
        }
    }

    /// <summary>
    /// Lista todas as redes de ativos coletadas
    /// </summary>
    /// <returns>Lista de redes de ativos coletadas</returns>
    [HttpGet("asset-networks")]
    public async Task<IActionResult> GetAssetNetworks()
    {
        try
        {
            var assetNetworks = await _dbContext.AssetNetworks
                .OrderBy(an => an.Asset)
                .ThenBy(an => an.Network)
                .ToListAsync();
            return Ok(assetNetworks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting asset networks");
            return StatusCode(500, "Error getting asset networks");
        }
    }
}
