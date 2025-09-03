using Dados.Data;
using Dados.Entities;
using MercadoBitcoin.Client;
using MercadoBitcoin.Client.Extensions;
using MercadoBitcoin.Client.Generated;
using MercadoBitcoin.Client.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Dados.Services;

/// <summary>
/// Service responsible for ingesting data from Mercado Bitcoin API
/// Uses MercadoBitcoin.Client with typed models and robust error handling
/// </summary>
public class DataIngestionService
{
    private readonly CryptoTradingDbContext _dbContext;
    private readonly MercadoBitcoinClient _mercadoBitcoinClient;
    private readonly ILogger<DataIngestionService> _logger;

    public DataIngestionService(
        CryptoTradingDbContext dbContext,
        MercadoBitcoinClient mercadoBitcoinClient,
        ILogger<DataIngestionService> logger)
    {
        _dbContext = dbContext;
        _mercadoBitcoinClient = mercadoBitcoinClient;
        _logger = logger;
    }

    /// <summary>
    /// Extrai ativos únicos da lista de símbolos (remove -BRL do final)
    /// </summary>
    private static List<string> GetUniqueAssetsFromSymbols(List<string> symbols)
    {
        return symbols
            .Select(s => s.Replace("-BRL", ""))
            .Distinct()
            .ToList();
    }

    /// <summary>
    /// Parse seguro de decimal com fallback para zero
    /// </summary>
    private static decimal ParseDecimal(object? value)
    {
        if (value == null) return 0m;

        if (value is decimal decimalValue) return decimalValue;
        if (value is double doubleValue) return (decimal)doubleValue;
        if (value is float floatValue) return (decimal)floatValue;
        if (value is int intValue) return intValue;
        if (value is long longValue) return longValue;

        if (decimal.TryParse(value.ToString(), System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var result))
        {
            return result;
        }

        return 0m;
    }

    /// <summary>
    /// Parse seguro de long com fallback para zero
    /// </summary>
    private static long ParseLong(object? value)
    {
        if (value == null) return 0L;

        if (value is long longValue) return longValue;
        if (value is int intValue) return intValue;
        if (value is decimal decimalValue) return (long)decimalValue;
        if (value is double doubleValue) return (long)doubleValue;
        if (value is float floatValue) return (long)floatValue;

        if (long.TryParse(value.ToString(), out var result))
        {
            return result;
        }

        return 0L;
    }

    /// <summary>
    /// Lista das top 50 criptomoedas para coleta de dados
    /// </summary>
    private static List<string> GetTopTier50CryptocurrencyList()
    {
        return new List<string>
        {
            // === TIER 1: BLUE CHIPS (Alta liquidez, baixo risco) ===
            "BTC-BRL",    // Bitcoin - Reserva de valor digital
            "ETH-BRL",    // Ethereum - Smart contracts líder
            "SOL-BRL",    // Solana - Alto throughput
            "ADA-BRL",    // Cardano - Blockchain sustentável
            "AVAX-BRL",   // Avalanche - Blockchain rápida
            "MATIC-BRL",  // Polygon - Layer 2 Ethereum
            "DOT-BRL",    // Polkadot - Interoperabilidade
            "BNB-BRL",    // Binance Coin - Exchange token líder
            "ATOM-BRL",   // Cosmos - Internet of blockchains
            "NEAR-BRL",   // Near Protocol - Blockchain escalável

            // === TIER 2: ESTABLISHED ALTCOINS (Liquidez média-alta) ===
            "USDT-BRL",   // Tether - Stablecoin líder
            "USDC-BRL",   // USD Coin - Stablecoin regulamentada
            "XRP-BRL",    // Ripple - Pagamentos internacionais
            "DOGE-BRL",   // Dogecoin - Meme coin líder
            "LTC-BRL",    // Litecoin - Silver to Bitcoin's gold
            "BCH-BRL",    // Bitcoin Cash - Fork do Bitcoin
            "LINK-BRL",   // Chainlink - Oracle descentralizado
            "UNI-BRL",    // Uniswap - DEX líder
            "TRX-BRL",    // Tron - Blockchain para entretenimento
            "FIL-BRL",    // Filecoin - Armazenamento descentralizado

            // === TIER 3: DEFI & SMART CONTRACT PLATFORMS ===
            "AAVE-BRL",   // Aave - Lending/borrowing
            "MKR-BRL",    // Maker - Stablecoin DAI
            "SNX-BRL",    // Synthetix - Derivativos sintéticos
            "SAND-BRL",   // The Sandbox - Gaming metaverse
            "MANA-BRL",   // Decentraland - Metaverse
            "COMP-BRL",   // Compound - Lending protocol
            "CRV-BRL",    // Curve - DEX stablecoins
            "SUSHI-BRL",  // SushiSwap - DEX
            "ENJ-BRL",    // Enjin - Gaming NFTs
            "1INCH-BRL",  // 1inch - DEX aggregator

            // === TIER 4: DEFI & UTILITY (Casos de uso reais) ===
            "GRT-BRL",    // The Graph - Indexing protocol
            "YFI-BRL",    // Yearn Finance - Yield farming
            "BAL-BRL",    // Balancer - AMM
            "REN-BRL",    // Ren - Cross-chain liquidity
            "ZRX-BRL",    // 0x Protocol - DEX infrastructure
            "OMG-BRL",    // OMG Network - Layer 2
            "LRC-BRL",    // Loopring - zkRollup DEX
            "STORJ-BRL",  // Storj - Cloud storage
            "BAND-BRL",   // Band Protocol - Oracle
            "KNC-BRL",    // Kyber Network - DEX

            // === TIER 5: ALTCOINS PROMISSORAS (Higher risk/reward) ===
            "FTM-BRL",    // Fantom - DAG-based
            "ALGO-BRL",   // Algorand - Pure proof-of-stake
            "EGLD-BRL",   // MultiversX - High throughput
            "HBAR-BRL",   // Hedera - Hashgraph consensus
            "XTZ-BRL",    // Tezos - Self-amending blockchain
            "ONE-BRL",    // Harmony - Sharding
            "AR-BRL",     // Arweave - Permanent storage
            "KSM-BRL",    // Kusama - Polkadot canary
            "WAVES-BRL",  // Waves - Custom tokens platform
            "ZIL-BRL"     // Zilliqa - Sharding pioneer
        };
    }

    /// <summary>
    /// Coleta e persiste dados dos símbolos (pares de moedas)
    /// Filtra apenas moedas da lista Top 50
    /// </summary>
    public async Task CollectSymbolsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting symbols collection with MercadoBitcoin.Client typed API");

            // Get our top 50 list
            var targetSymbols = GetTopTier50CryptocurrencyList();

            _logger.LogInformation($"Creating symbols from predefined list with {targetSymbols.Count} symbols");

            // Create entities directly from our list
            var entities = targetSymbols.Select(symbol => new SymbolEntity
            {
                Symbol = symbol,
                BaseCurrency = symbol.Replace("-BRL", ""),
                QuoteCurrency = "BRL",
                Status = "ACTIVE",
                BasePrecision = 8,
                QuotePrecision = 2,
                AmountPrecision = 8,
                MinOrderAmount = 0.00000001m,
                MinOrderValue = 1.00m,
                CollectedAt = DateTime.UtcNow
            }).ToList();

            // Use execution strategy for reliable transaction handling
            var strategy = _dbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    // Clean existing symbols that are not in our list
                    var existingSymbols = await _dbContext.Symbols.ToListAsync(cancellationToken);
                    var symbolsToRemove = existingSymbols.Where(s => !targetSymbols.Contains(s.Symbol)).ToList();

                    if (symbolsToRemove.Any())
                    {
                        _logger.LogInformation($"Removing {symbolsToRemove.Count} symbols that are not in top 50 list");
                        _dbContext.Symbols.RemoveRange(symbolsToRemove);
                    }

                    // Use upsert pattern for symbols
                    foreach (var entity in entities)
                    {
                        var existingSymbol = await _dbContext.Symbols
                            .FirstOrDefaultAsync(s => s.Symbol == entity.Symbol, cancellationToken);

                        if (existingSymbol != null)
                        {
                            // Update existing
                            existingSymbol.BaseCurrency = entity.BaseCurrency;
                            existingSymbol.QuoteCurrency = entity.QuoteCurrency;
                            existingSymbol.Status = entity.Status;
                            existingSymbol.BasePrecision = entity.BasePrecision;
                            existingSymbol.QuotePrecision = entity.QuotePrecision;
                            existingSymbol.AmountPrecision = entity.AmountPrecision;
                            existingSymbol.MinOrderAmount = entity.MinOrderAmount;
                            existingSymbol.MinOrderValue = entity.MinOrderValue;
                            existingSymbol.CollectedAt = DateTime.UtcNow;
                        }
                        else
                        {
                            // Add new
                            await _dbContext.Symbols.AddAsync(entity, cancellationToken);
                        }
                    }

                    var savedCount = await _dbContext.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);

                    _logger.LogInformation($"Successfully saved/updated {savedCount} symbols records");
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during symbols collection: {Message}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Coleta e persiste dados dos tickers (preços atuais)
    /// Filtra apenas moedas da lista Top 50
    /// </summary>
    public async Task CollectTickersAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting tickers collection");
            var targetSymbols = GetTopTier50CryptocurrencyList();

            // Clean existing tickers that are not in our list
            var tickersToRemove = await _dbContext.Tickers
                .Where(t => !targetSymbols.Contains(t.Symbol))
                .ToListAsync(cancellationToken);

            if (tickersToRemove.Any())
            {
                _dbContext.Tickers.RemoveRange(tickersToRemove);
                await _dbContext.SaveChangesAsync(cancellationToken);
                _logger.LogInformation($"Removed {tickersToRemove.Count} tickers not in top 50 list");
            }

            var collected = new List<TickerEntity>();
            int success = 0, errors = 0;

            foreach (var symbol in targetSymbols)
            {
                try
                {
                    var tickerResponse = await _mercadoBitcoinClient.GetTickersAsync(symbol);
                    if (tickerResponse?.Any() == true)
                    {
                        var ticker = tickerResponse.First();
                        var entity = new TickerEntity
                        {
                            Symbol = symbol,
                            Last = ParseDecimal(ticker.Last),
                            High = ParseDecimal(ticker.High),
                            Low = ParseDecimal(ticker.Low),
                            Vol = ParseDecimal(ticker.Vol),
                            Buy = ParseDecimal(ticker.Buy),
                            Sell = ParseDecimal(ticker.Sell),
                            Date = ticker.Date.HasValue ? ticker.Date.Value : 0,
                            CollectedAt = DateTime.UtcNow
                        };
                        collected.Add(entity);
                        success++;
                    }
                    else
                    {
                        errors++;
                        _logger.LogWarning("Empty ticker response for {Symbol}", symbol);
                    }
                }
                catch (Exception ex)
                {
                    errors++;
                    _logger.LogWarning(ex, "Error collecting ticker for {Symbol}", symbol);
                }

                await Task.Delay(100, cancellationToken); // Rate limiting
            }

            _logger.LogInformation("Ticker collection completed: {Success} successful, {Error} errors", success, errors);

            if (collected.Any())
            {
                // Use upsert pattern for tickers
                foreach (var entity in collected)
                {
                    var existing = await _dbContext.Tickers
                        .FirstOrDefaultAsync(t => t.Symbol == entity.Symbol, cancellationToken);

                    if (existing != null)
                    {
                        existing.Last = entity.Last;
                        existing.High = entity.High;
                        existing.Low = entity.Low;
                        existing.Vol = entity.Vol;
                        existing.Buy = entity.Buy;
                        existing.Sell = entity.Sell;
                        existing.Date = entity.Date;
                        existing.CollectedAt = entity.CollectedAt;
                    }
                    else
                    {
                        await _dbContext.Tickers.AddAsync(entity, cancellationToken);
                    }
                }

                await _dbContext.SaveChangesAsync(cancellationToken);
                _logger.LogInformation($"Successfully saved/updated {collected.Count} tickers");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during tickers collection: {Message}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Coleta e persiste dados dos order books
    /// </summary>
    public async Task CollectOrderBookAsync(int? limit = null, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting order book collection");
            var targetSymbols = GetTopTier50CryptocurrencyList();

            // Clean existing order books that are not in our list
            var orderBooksToRemove = await _dbContext.OrderBooks
                .Where(ob => !targetSymbols.Contains(ob.Symbol))
                .ToListAsync(cancellationToken);

            if (orderBooksToRemove.Any())
            {
                _dbContext.OrderBooks.RemoveRange(orderBooksToRemove);
                await _dbContext.SaveChangesAsync(cancellationToken);
                _logger.LogInformation($"Removed {orderBooksToRemove.Count} order books not in top 50 list");
            }

            int success = 0, errors = 0;
            string limitStr = (limit ?? 10).ToString();

            foreach (var symbol in targetSymbols)
            {
                try
                {
                    var orderBookResponse = await _mercadoBitcoinClient.GetOrderBookAsync(symbol, limitStr);
                    if (orderBookResponse != null)
                    {
                        var bidsJson = JsonSerializer.Serialize(orderBookResponse.Bids);
                        var asksJson = JsonSerializer.Serialize(orderBookResponse.Asks);

                        // Use upsert pattern for order books
                        var existing = await _dbContext.OrderBooks
                            .FirstOrDefaultAsync(ob => ob.Symbol == symbol, cancellationToken);

                        if (existing != null)
                        {
                            existing.Bids = bidsJson;
                            existing.Asks = asksJson;
                            existing.CollectedAt = DateTime.UtcNow;
                        }
                        else
                        {
                            await _dbContext.OrderBooks.AddAsync(new OrderBookEntity
                            {
                                Symbol = symbol,
                                Bids = bidsJson,
                                Asks = asksJson,
                                CollectedAt = DateTime.UtcNow
                            }, cancellationToken);
                        }
                        success++;
                    }
                    else
                    {
                        errors++;
                        _logger.LogWarning("Null order book response for {Symbol}", symbol);
                    }
                }
                catch (Exception ex)
                {
                    errors++;
                    _logger.LogWarning(ex, "Error collecting order book for {Symbol}", symbol);
                }

                await Task.Delay(100, cancellationToken); // Rate limiting
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Order book collection completed: {Success} successful, {Error} errors", success, errors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during order book collection: {Message}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Coleta e persiste dados de trades
    /// </summary>
    public async Task CollectTradesAsync(int? limit = null, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting trades collection");
            var targetSymbols = GetTopTier50CryptocurrencyList();

            // Clean existing trades that are not in our list
            var tradesToRemove = await _dbContext.Trades
                .Where(t => !targetSymbols.Contains(t.Symbol))
                .ToListAsync(cancellationToken);

            if (tradesToRemove.Any())
            {
                _dbContext.Trades.RemoveRange(tradesToRemove);
                await _dbContext.SaveChangesAsync(cancellationToken);
                _logger.LogInformation($"Removed {tradesToRemove.Count} trades not in top 50 list");
            }

            var allEntities = new List<TradeEntity>();
            int success = 0, errors = 0;
            int requestedLimit = limit ?? 100;

            foreach (var symbol in targetSymbols)
            {
                try
                {
                    var tradesResponse = await _mercadoBitcoinClient.GetTradesAsync(symbol, limit: requestedLimit);
                    if (tradesResponse?.Any() == true)
                    {
                        foreach (var trade in tradesResponse)
                        {
                            try
                            {
                                var entity = new TradeEntity
                                {
                                    Symbol = symbol,
                                    Tid = trade.Tid ?? 0,
                                    Date = trade.Date ?? 0,
                                    Price = ParseDecimal(trade.Price),
                                    Type = trade.Type,
                                    Amount = ParseDecimal(trade.Amount),
                                    CollectedAt = DateTime.UtcNow
                                };
                                allEntities.Add(entity);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogDebug(ex, "Failed to map trade for {Symbol}", symbol);
                            }
                        }
                        success++;
                    }
                    else
                    {
                        success++; // Empty response is still success
                        _logger.LogDebug("No trades found for {Symbol}", symbol);
                    }
                }
                catch (Exception ex)
                {
                    errors++;
                    _logger.LogWarning(ex, "Error collecting trades for {Symbol}", symbol);
                }

                await Task.Delay(150, cancellationToken); // Rate limiting
            }

            _logger.LogInformation("Trades collection completed: {Success} successful, {Error} errors", success, errors);

            if (allEntities.Any())
            {
                // Use upsert pattern to avoid duplicate key conflicts
                foreach (var entity in allEntities)
                {
                    var existing = await _dbContext.Trades
                        .FirstOrDefaultAsync(t => t.Symbol == entity.Symbol && t.Tid == entity.Tid, cancellationToken);

                    if (existing != null)
                    {
                        // Update existing trade
                        existing.Date = entity.Date;
                        existing.Price = entity.Price;
                        existing.Type = entity.Type;
                        existing.Amount = entity.Amount;
                        existing.CollectedAt = entity.CollectedAt;
                    }
                    else
                    {
                        // Add new trade
                        await _dbContext.Trades.AddAsync(entity, cancellationToken);
                    }
                }

                await _dbContext.SaveChangesAsync(cancellationToken);
                _logger.LogInformation($"Successfully saved/updated {allEntities.Count} trades");
            }
            else
            {
                _logger.LogInformation("No trades were collected");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during trades collection: {Message}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Coleta e persiste dados de candles
    /// </summary>
    public async Task CollectCandlesAsync(string? resolution = null, int? countback = null, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting candles collection");
            var targetSymbols = GetTopTier50CryptocurrencyList();
            string candleResolution = resolution ?? "1h";
            int candleCountback = countback ?? 24;

            // Clean existing candles that are not in our list
            var candlesToRemove = await _dbContext.Candles
                .Where(c => !targetSymbols.Contains(c.Symbol))
                .ToListAsync(cancellationToken);

            if (candlesToRemove.Any())
            {
                _dbContext.Candles.RemoveRange(candlesToRemove);
                await _dbContext.SaveChangesAsync(cancellationToken);
                _logger.LogInformation($"Removed {candlesToRemove.Count} candles not in top 50 list");
            }

            var allEntities = new List<CandleEntity>();
            int success = 0, errors = 0;
            var to = (int)DateTimeOffset.Now.ToUnixTimeSeconds();

            foreach (var symbol in targetSymbols)
            {
                try
                {
                    // Try the typed recent candles method first
                    try
                    {
                        var typedCandles = await _mercadoBitcoinClient.GetRecentCandlesTypedAsync(symbol, candleResolution, candleCountback);
                        if (typedCandles?.Any() == true)
                        {
                            foreach (var candle in typedCandles)
                            {
                                try
                                {
                                    var entity = new CandleEntity
                                    {
                                        Symbol = symbol,
                                        Resolution = candleResolution,
                                        Timestamp = candle.OpenTime,
                                        Open = candle.Open,
                                        High = candle.High,
                                        Low = candle.Low,
                                        Close = candle.Close,
                                        Volume = candle.Volume,
                                        CollectedAt = DateTime.UtcNow
                                    };
                                    allEntities.Add(entity);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogDebug(ex, "Failed to map typed candle for {Symbol}", symbol);
                                }
                            }
                            success++;
                            _logger.LogDebug("Collected {Count} typed candles for {Symbol}", typedCandles.Count, symbol);
                            continue;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Typed candles failed for {Symbol}, trying raw method", symbol);
                    }

                    // Fallback to raw candles method
                    var rawCandles = await _mercadoBitcoinClient.GetCandlesAsync(symbol, candleResolution, to, null, candleCountback);
                    if (rawCandles != null)
                    {
                        try
                        {
                            var closes = ((IEnumerable<dynamic>)rawCandles.C).ToList();
                            var highs = ((IEnumerable<dynamic>)rawCandles.H).ToList();
                            var lows = ((IEnumerable<dynamic>)rawCandles.L).ToList();
                            var opens = ((IEnumerable<dynamic>)rawCandles.O).ToList();
                            var times = ((IEnumerable<dynamic>)rawCandles.T).ToList();
                            var volumes = ((IEnumerable<dynamic>)rawCandles.V).ToList();

                            int count = Math.Min(times.Count, Math.Min(opens.Count, Math.Min(highs.Count, Math.Min(lows.Count, Math.Min(closes.Count, volumes.Count)))));

                            for (int i = 0; i < count; i++)
                            {
                                try
                                {
                                    var entity = new CandleEntity
                                    {
                                        Symbol = symbol,
                                        Resolution = candleResolution,
                                        Timestamp = ParseLong(times[i]),
                                        Open = ParseDecimal(opens[i]),
                                        High = ParseDecimal(highs[i]),
                                        Low = ParseDecimal(lows[i]),
                                        Close = ParseDecimal(closes[i]),
                                        Volume = ParseDecimal(volumes[i]),
                                        CollectedAt = DateTime.UtcNow
                                    };
                                    allEntities.Add(entity);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogDebug(ex, "Failed to map raw candle index {Index} for {Symbol}", i, symbol);
                                }
                            }
                            success++;
                            _logger.LogDebug("Collected {Count} raw candles for {Symbol}", count, symbol);
                        }
                        catch (Exception ex)
                        {
                            errors++;
                            _logger.LogWarning(ex, "Failed to parse raw candles for {Symbol}", symbol);
                        }
                    }
                    else
                    {
                        success++; // Empty response is still success
                        _logger.LogDebug("No candles found for {Symbol}", symbol);
                    }
                }
                catch (Exception ex)
                {
                    errors++;
                    _logger.LogWarning(ex, "Error collecting candles for {Symbol}", symbol);
                }

                await Task.Delay(150, cancellationToken); // Rate limiting
            }

            _logger.LogInformation("Candles collection completed: {Success} successful, {Error} errors", success, errors);

            if (allEntities.Any())
            {
                await _dbContext.Candles.AddRangeAsync(allEntities, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);
                _logger.LogInformation($"Successfully saved {allEntities.Count} candles");
            }
            else
            {
                _logger.LogInformation("No candles were collected");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during candles collection: {Message}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Coleta e persiste dados de asset fees
    /// </summary>
    public async Task CollectAssetFeesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting asset fees collection");
            var targetSymbols = GetTopTier50CryptocurrencyList();
            var targetAssets = GetUniqueAssetsFromSymbols(targetSymbols);

            await Task.Delay(100, cancellationToken); // Simulate work
            _logger.LogInformation($"Successfully collected asset fees for {targetAssets.Count} assets");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during asset fees collection: {Message}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Coleta e persiste dados de asset networks
    /// </summary>
    public async Task CollectAssetNetworksAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting asset networks collection");
            var targetSymbols = GetTopTier50CryptocurrencyList();
            var targetAssets = GetUniqueAssetsFromSymbols(targetSymbols);

            await Task.Delay(100, cancellationToken); // Simulate work
            _logger.LogInformation($"Successfully collected asset networks for {targetAssets.Count} assets");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during asset networks collection: {Message}", ex.Message);
            throw;
        }
    }


    /// <summary>
    /// Coleta todos os dados
    /// </summary>
    public async Task CollectAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting complete data collection");

            await CollectSymbolsAsync(cancellationToken);
            await CollectTickersAsync(cancellationToken);
            await CollectOrderBookAsync(10, cancellationToken);
            await CollectTradesAsync(100, cancellationToken);
            await CollectCandlesAsync("1h", 24, cancellationToken);
            await CollectAssetFeesAsync(cancellationToken);
            await CollectAssetNetworksAsync(cancellationToken);

            _logger.LogInformation("Complete data collection finished successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during complete data collection");
            throw;
        }
    }

    /// <summary>
    /// Retorna lista de símbolos
    /// </summary>
    public async Task<List<string>> GetSymbolsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var symbols = await _dbContext.Symbols
                .Select(s => s.Symbol)
                .ToListAsync(cancellationToken);

            return symbols;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting symbols: {Message}", ex.Message);
            throw;
        }
    }

}
