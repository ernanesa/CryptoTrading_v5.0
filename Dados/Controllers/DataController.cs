using Dados.Services;
using Microsoft.AspNetCore.Mvc;

namespace Dados.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DataController : ControllerBase
{
    private readonly DataIngestionService _dataIngestionService;
    private readonly ILogger<DataController> _logger;

    public DataController(
        DataIngestionService dataIngestionService,
        ILogger<DataController> logger)
    {
        _dataIngestionService = dataIngestionService;
        _logger = logger;
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
    public async Task<IActionResult> CollectOrderBooks([FromQuery] string limit = "10", CancellationToken cancellationToken = default)
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
}
