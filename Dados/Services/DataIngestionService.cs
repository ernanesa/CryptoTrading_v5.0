using Dados.Data;
using Dados.Entities;
using MercadoBitcoin.Client;
using MercadoBitcoin.Client.Extensions;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
// removed unused usings

namespace Dados.Services;

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

            // === TIER 2: LARGE CAPS (Boa liquidez, médio risco) ===
            "LINK-BRL",   // Chainlink - Oracles líder
            "UNI-BRL",    // Uniswap - DEX líder
            "LTC-BRL",    // Litecoin - Silver to Bitcoin's gold
            "XRP-BRL",    // Ripple - Pagamentos internacionais
            "TRX-BRL",    // Tron - Entertainment blockchain
            "VET-BRL",    // VeChain - Supply chain
            "FIL-BRL",    // Filecoin - Armazenamento descentralizado
            "THETA-BRL",  // Theta - Video streaming
            "ICP-BRL",    // Internet Computer - Cloud descentralizado
            "BCH-BRL",    // Bitcoin Cash - Fork do Bitcoin

            // === TIER 3: MID CAPS SÓLIDOS (Trading ativo) ===
            "AAVE-BRL",   // Aave - Lending DeFi
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

    public async Task CollectSymbolsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Obter a lista das 50 moedas selecionadas
            var selectedSymbols = GetTopTier50CryptocurrencyList();
            _logger.LogInformation("Starting collection for {Count} selected symbols", selectedSymbols.Count);

            // Criar entidades diretamente da lista selecionada
            var filteredEntities = selectedSymbols.Select(symbol => new SymbolEntity
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

            _logger.LogInformation("Created {Count} entities from selected symbols", filteredEntities.Count);

            // Usar transação com estratégia de execução
            var strategy = _dbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    // Primeiro, remover todos os símbolos existentes usando SQL direto
                    await _dbContext.Database.ExecuteSqlRawAsync("DELETE FROM \"Symbols\"", cancellationToken);
                    _logger.LogInformation("Deleted all existing symbols using raw SQL");

                    // Limpar o contexto para evitar conflitos
                    _dbContext.ChangeTracker.Clear();

                    // Agora adicionar apenas os símbolos filtrados
                    await _dbContext.Symbols.AddRangeAsync(filteredEntities, cancellationToken);
                    await _dbContext.SaveChangesAsync(cancellationToken);

                    await transaction.CommitAsync(cancellationToken);
                    _logger.LogInformation("Collected {Count} symbols from the predefined list of {TotalSelected} symbols", filteredEntities.Count, selectedSymbols.Count);
                }
                catch
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting symbols");
            throw;
        }
    }

    public async Task CollectTickersAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var activeSymbols = GetTopTier50CryptocurrencyList();
            if (!activeSymbols.Any())
            {
                _logger.LogWarning("No symbols found in the predefined list");
                return;
            }

            _logger.LogInformation("Starting ticker collection for {Count} symbols (per-symbol requests)", activeSymbols.Count);

            // Limpar tickers de símbolos fora da lista
            var toRemove = await _dbContext.Tickers.Where(t => !activeSymbols.Contains(t.Symbol)).ToListAsync(cancellationToken);
            if (toRemove.Any())
            {
                _dbContext.Tickers.RemoveRange(toRemove);
                await _dbContext.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Removed {Count} obsolete tickers", toRemove.Count);
            }

            var collected = new List<TickerEntity>();
            int success = 0, errors = 0;
            foreach (var symbol in activeSymbols)
            {
                try
                {
                    var start = DateTime.UtcNow;
                    dynamic resp = await _mercadoBitcoinClient.GetTickersAsync(symbol);
                    var ms = (DateTime.UtcNow - start).TotalMilliseconds;
                    if (resp == null)
                    {
                        errors++;
                        _logger.LogWarning("Null ticker response for {Symbol}", symbol);
                        continue;
                    }
                    // Caso a API retorne array
                    dynamic data = resp;
                    if (resp is IEnumerable<dynamic> arr)
                    {
                        var first = arr.FirstOrDefault();
                        if (first == null)
                        {
                            errors++;
                            _logger.LogWarning("Empty array ticker response for {Symbol}", symbol);
                            continue;
                        }
                        data = first;
                    }
                    var entity = new TickerEntity
                    {
                        Symbol = symbol,
                        Last = ParseDecimal(data?.Last ?? data?.last),
                        High = ParseDecimal(data?.High ?? data?.high),
                        Low = ParseDecimal(data?.Low ?? data?.low),
                        Vol = ParseDecimal(data?.Vol ?? data?.vol),
                        Buy = ParseDecimal(data?.Buy ?? data?.buy),
                        Sell = ParseDecimal(data?.Sell ?? data?.sell),
                        Date = ParseLong(data?.Date ?? data?.date),
                        CollectedAt = DateTime.UtcNow
                    };
                    collected.Add(entity);
                    success++;
                    _logger.LogInformation("Ticker {Symbol} OK in {Ms}ms", symbol, ms);
                }
                catch (Exception ex)
                {
                    errors++;
                    _logger.LogWarning(ex, "Error collecting ticker for {Symbol}", symbol);
                }
                await Task.Delay(120, cancellationToken); // leve throttle
            }

            _logger.LogInformation("Ticker summary Success={Success} Errors={Errors}", success, errors);
            if (!collected.Any())
            {
                _logger.LogWarning("No tickers collected");
                return;
            }

            var strategy = _dbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                using var tx = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    foreach (var e in collected)
                    {
                        var existing = await _dbContext.Tickers.FindAsync(new object[] { e.Symbol }, cancellationToken);
                        if (existing == null)
                        {
                            await _dbContext.Tickers.AddAsync(e, cancellationToken);
                        }
                        else
                        {
                            existing.Last = e.Last;
                            existing.High = e.High;
                            existing.Low = e.Low;
                            existing.Vol = e.Vol;
                            existing.Buy = e.Buy;
                            existing.Sell = e.Sell;
                            existing.Date = e.Date;
                            existing.CollectedAt = e.CollectedAt;
                        }
                    }
                    await _dbContext.SaveChangesAsync(cancellationToken);
                    await tx.CommitAsync(cancellationToken);
                    _logger.LogInformation("Persisted {Count} tickers", collected.Count);
                }
                catch
                {
                    await tx.RollbackAsync(cancellationToken);
                    throw;
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting tickers");
            throw;
        }
    }

    public async Task CollectAllAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("=== START FULL INGESTION ===");
        await CollectSymbolsAsync(cancellationToken);
        await CollectTickersAsync(cancellationToken);
        await CollectOrderBookAsync("10", cancellationToken);
        await CollectTradesAsync(null, cancellationToken);
        await CollectCandlesAsync("1h", 24, cancellationToken);
        await CollectAssetFeesAsync(cancellationToken);
        await CollectAssetNetworksAsync(cancellationToken);
        _logger.LogInformation("=== END FULL INGESTION ===");
    }

    public async Task CollectOrderBookAsync(string limit = "10", CancellationToken cancellationToken = default)
    {
        try
        {
            // Usar lista fixa das 50 melhores criptomoedas
            var activeSymbols = GetTopTier50CryptocurrencyList();

            if (!activeSymbols.Any())
            {
                _logger.LogWarning("No symbols found in the predefined list");
                return;
            }

            // Limpar order books de símbolos fora da lista
            var orderBooksToRemove = await _dbContext.OrderBooks
                .Where(ob => !activeSymbols.Contains(ob.Symbol))
                .ToListAsync(cancellationToken);
            if (orderBooksToRemove.Any())
            {
                _dbContext.OrderBooks.RemoveRange(orderBooksToRemove);
                await _dbContext.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Removed {Count} obsolete order books", orderBooksToRemove.Count);
            }

            // Remover duplicados mantendo o mais recente por símbolo (PostgreSQL window function)
            try
            {
                var sqlCleanup = "DELETE FROM \"OrderBooks\" ob USING (SELECT \"Id\", ROW_NUMBER() OVER (PARTITION BY \"Symbol\" ORDER BY \"CollectedAt\" DESC) rn FROM \"OrderBooks\") dup WHERE ob.\"Id\" = dup.\"Id\" AND dup.rn > 1;";
                await _dbContext.Database.ExecuteSqlRawAsync(sqlCleanup, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Duplicate cleanup for order books failed (non-fatal)");
            }

            int success = 0, errors = 0;
            foreach (var symbol in activeSymbols)
            {
                try
                {
                    dynamic orderBookResponse = await _mercadoBitcoinClient.GetOrderBookAsync(symbol, limit);
                    if (orderBookResponse == null)
                    {
                        errors++; continue;
                    }
                    var bidsJson = JsonSerializer.Serialize(orderBookResponse.Bids);
                    var asksJson = JsonSerializer.Serialize(orderBookResponse.Asks);
                    // Upsert manual
                    var existing = await _dbContext.OrderBooks.FirstOrDefaultAsync(o => o.Symbol == symbol, cancellationToken);
                    if (existing == null)
                    {
                        await _dbContext.OrderBooks.AddAsync(new OrderBookEntity
                        {
                            Symbol = symbol,
                            Bids = bidsJson,
                            Asks = asksJson,
                            CollectedAt = DateTime.UtcNow
                        }, cancellationToken);
                    }
                    else
                    {
                        existing.Bids = bidsJson;
                        existing.Asks = asksJson;
                        existing.CollectedAt = DateTime.UtcNow;
                    }
                    success++;
                }
                catch (Exception ex)
                {
                    errors++;
                    _logger.LogWarning(ex, "Error collecting order book for symbol {Symbol}", symbol);
                }
                await Task.Delay(120, cancellationToken);
            }
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("OrderBook summary Success={Success} Errors={Errors}", success, errors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting order books");
            throw;
        }
    }

    public async Task CollectTradesAsync(int? limit = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("=== COLLECT TRADES STARTED ===");
        try
        {
            _logger.LogInformation("Starting trade collection process");

            // Usar lista fixa das 50 melhores criptomoedas
            var activeSymbols = GetTopTier50CryptocurrencyList();
            _logger.LogInformation("Found {Count} active symbols for trade collection", activeSymbols.Count);

            if (!activeSymbols.Any())
            {
                _logger.LogWarning("No symbols found in the predefined list");
                return;
            }

            // Limpar trades de símbolos que não estão na lista selecionada
            var tradesToRemove = await _dbContext.Trades
                .Where(t => !activeSymbols.Contains(t.Symbol))
                .ToListAsync(cancellationToken);

            if (tradesToRemove.Any())
            {
                _dbContext.Trades.RemoveRange(tradesToRemove);
                await _dbContext.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Removed {Count} trades from symbols not in the selected list", tradesToRemove.Count);
            }

            var allEntities = new List<TradeEntity>();
            var successCount = 0;
            var errorCount = 0;

            int requestedLimit = limit ?? 100; // default
            foreach (var symbol in activeSymbols)
            {
                try
                {
                    _logger.LogInformation("Collecting trades for {Symbol} (limit {Limit})", symbol, requestedLimit);
                    var swStart = DateTime.UtcNow;
                    var tradesResponse = await _mercadoBitcoinClient.GetTradesAsync(symbol, limit: requestedLimit);
                    var ms = (DateTime.UtcNow - swStart).TotalMilliseconds;
                    if (tradesResponse == null)
                    {
                        errorCount++;
                        _logger.LogWarning("Null trades response for {Symbol}", symbol);
                        continue;
                    }
                    var list = tradesResponse.ToList();
                    _logger.LogInformation("Received {Count} trades for {Symbol} in {Ms}ms", list.Count, symbol, ms);
                    if (list.Count == 0)
                    {
                        successCount++; // conta como sucesso vazio
                        continue;
                    }
                    foreach (var t in list)
                    {
                        try
                        {
                            var entity = new TradeEntity
                            {
                                Symbol = symbol,
                                Tid = (int)(t.Tid ?? 0),
                                Date = SafeToLong(t.Date),
                                Price = ParseDecimal(t.Price),
                                Type = t.Type,
                                Amount = ParseDecimal(t.Amount),
                                CollectedAt = DateTime.UtcNow
                            };
                            allEntities.Add(entity);
                        }
                        catch (Exception inner)
                        {
                            errorCount++;
                            _logger.LogWarning(inner, "Skipping malformed trade for {Symbol}", symbol);
                        }
                    }
                    successCount++;
                }
                catch (Exception ex)
                {
                    errorCount++;
                    _logger.LogWarning(ex, "Error collecting trades for {Symbol} ({Error}/{Total})", symbol, errorCount, activeSymbols.Count);
                }

                await Task.Delay(200, cancellationToken); // leve espaçamento
            }

            _logger.LogInformation("Trade collection completed: {Success} successful, {Error} errors out of {Total} symbols", successCount, errorCount, activeSymbols.Count);

            if (allEntities.Any())
            {
                _logger.LogInformation("Attempting to save {Count} trades to database", allEntities.Count);

                await _dbContext.Trades.AddRangeAsync(allEntities, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Saved {Count} trades to database", allEntities.Count);
            }
            else
            {
                _logger.LogWarning("No trades were collected successfully");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting trades");
            throw;
        }
    }

    public async Task CollectCandlesAsync(string resolution = "1h", int? countback = 24, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting candle collection process with resolution {Resolution}", resolution);

            // Usar lista fixa das 50 melhores criptomoedas
            var activeSymbols = GetTopTier50CryptocurrencyList();
            _logger.LogInformation("Found {Count} active symbols for candle collection", activeSymbols.Count);

            if (!activeSymbols.Any())
            {
                _logger.LogWarning("No symbols found in the predefined list");
                return;
            }

            // Limpar candles de símbolos que não estão na lista selecionada
            var candlesToRemove = await _dbContext.Candles
                .Where(c => !activeSymbols.Contains(c.Symbol))
                .ToListAsync(cancellationToken);

            if (candlesToRemove.Any())
            {
                _dbContext.Candles.RemoveRange(candlesToRemove);
                await _dbContext.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Removed {Count} candles from symbols not in the selected list", candlesToRemove.Count);
            }

            var to = (int)DateTimeOffset.Now.ToUnixTimeSeconds();
            var allEntities = new List<CandleEntity>();
            var successCount = 0;
            var errorCount = 0;

            foreach (var symbol in activeSymbols)
            {
                try
                {
                    _logger.LogInformation("Collecting candles for {Symbol}", symbol);
                    var start = DateTime.UtcNow;
                    // Preferir método tipado recente (fallback para o antigo se necessário)
                    IReadOnlyList<dynamic>? typed = null;
                    try
                    {
                        typed = await _mercadoBitcoinClient.GetRecentCandlesTypedAsync(symbol, resolution, countback ?? 24);
                    }
                    catch (Exception inner)
                    {
                        _logger.LogDebug(inner, "Typed candles fallback for {Symbol}", symbol);
                    }

                    if (typed != null)
                    {
                        foreach (var c in typed)
                        {
                            try
                            {
                                // Tentativa de propriedades comuns (Timestamp/Open/High/Low/Close/Volume)
                                long ts = c.Timestamp;
                                var entity = new CandleEntity
                                {
                                    Symbol = symbol,
                                    Resolution = resolution,
                                    Timestamp = ts,
                                    Open = c.Open,
                                    High = c.High,
                                    Low = c.Low,
                                    Close = c.Close,
                                    Volume = c.Volume,
                                    CollectedAt = DateTime.UtcNow
                                };
                                allEntities.Add(entity);
                            }
                            catch (Exception mapEx)
                            {
                                _logger.LogDebug(mapEx, "Failed to map typed candle for {Symbol}", symbol);
                            }
                        }
                        successCount++;
                        var ms = (DateTime.UtcNow - start).TotalMilliseconds;
                        _logger.LogInformation("Collected {Count} typed candles for {Symbol} in {Ms}ms", typed.Count, symbol, ms);
                        continue;
                    }

                    // Fallback para método bruto
                    dynamic raw = await _mercadoBitcoinClient.GetCandlesAsync(symbol, resolution, to, null, countback);
                    if (raw == null)
                    {
                        errorCount++;
                        continue;
                    }
                    var closes = ((IEnumerable<dynamic>)raw.c).ToList();
                    var highs = ((IEnumerable<dynamic>)raw.h).ToList();
                    var lows = ((IEnumerable<dynamic>)raw.l).ToList();
                    var opens = ((IEnumerable<dynamic>)raw.o).ToList();
                    var times = ((IEnumerable<dynamic>)raw.t).ToList();
                    var vols = ((IEnumerable<dynamic>)raw.v).ToList();
                    int count = times.Count;
                    for (int i = 0; i < count; i++)
                    {
                        try
                        {
                            var entity = new CandleEntity
                            {
                                Symbol = symbol,
                                Resolution = resolution,
                                Timestamp = long.Parse(times[i].ToString()),
                                Open = ParseDecimal(opens[i]),
                                High = ParseDecimal(highs[i]),
                                Low = ParseDecimal(lows[i]),
                                Close = ParseDecimal(closes[i]),
                                Volume = ParseDecimal(vols[i]),
                                CollectedAt = DateTime.UtcNow
                            };
                            allEntities.Add(entity);
                        }
                        catch (Exception cex)
                        {
                            _logger.LogDebug(cex, "Skipping malformed candle index {Index} for {Symbol}", i, symbol);
                        }
                    }
                    successCount++;
                    _logger.LogInformation("Collected {Count} raw candles for {Symbol}", count, symbol);
                }
                catch (Exception ex)
                {
                    errorCount++;
                    _logger.LogWarning(ex, "Error collecting candles for {Symbol} ({Error}/{Total})", symbol, errorCount, activeSymbols.Count);
                }
                await Task.Delay(200, cancellationToken);
            }

            _logger.LogInformation("Candle collection completed: {Success} successful, {Error} errors out of {Total} symbols", successCount, errorCount, activeSymbols.Count);

            if (allEntities.Any())
            {
                _logger.LogInformation("Attempting to save {Count} candles to database", allEntities.Count);

                await _dbContext.Candles.AddRangeAsync(allEntities, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Saved {Count} candles to database", allEntities.Count);
            }
            else
            {
                _logger.LogWarning("No candles were collected successfully");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting candles");
            throw;
        }
    }

    public async Task CollectAssetFeesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Usar lista fixa das 50 melhores criptomoedas e extrair ativos únicos
            var symbols = GetTopTier50CryptocurrencyList();
            var activeAssets = GetUniqueAssetsFromSymbols(symbols);

            if (!activeAssets.Any())
            {
                _logger.LogWarning("No assets found in the predefined list");
                return;
            }

            // Limpar asset fees de ativos que não estão na lista selecionada
            var assetFeesToRemove = await _dbContext.AssetFees
                .Where(af => !activeAssets.Contains(af.Asset))
                .ToListAsync(cancellationToken);

            if (assetFeesToRemove.Any())
            {
                _dbContext.AssetFees.RemoveRange(assetFeesToRemove);
                _logger.LogInformation("Removed {Count} asset fees from assets not in the selected list", assetFeesToRemove.Count);
            }

            var entities = new List<AssetFeeEntity>();

            foreach (var asset in activeAssets)
            {
                try
                {
                    dynamic feesResponse = await _mercadoBitcoinClient.GetAssetFeesAsync(asset);
                    var entity = new AssetFeeEntity
                    {
                        Asset = asset,
                        WithdrawalFee = decimal.Parse((string)feesResponse.Withdrawal_fee),
                        CollectedAt = DateTime.UtcNow
                    };
                    entities.Add(entity);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error collecting asset fees for {Asset}", asset);
                    // Continue com outros ativos
                }
            }

            if (entities.Any())
            {
                await _dbContext.AssetFees.AddRangeAsync(entities, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Collected asset fees for {Count} assets", entities.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting asset fees");
            throw;
        }
    }

    public async Task CollectAssetNetworksAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting asset networks collection process");

            // Usar lista fixa das 50 melhores criptomoedas e extrair ativos únicos
            var symbols = GetTopTier50CryptocurrencyList();
            var activeAssets = GetUniqueAssetsFromSymbols(symbols);
            _logger.LogInformation("Found {Count} active assets for network collection", activeAssets.Count);

            if (!activeAssets.Any())
            {
                _logger.LogWarning("No assets found in the predefined list");
                return;
            }

            // Limpar asset networks de ativos que não estão na lista selecionada
            var assetNetworksToRemove = await _dbContext.AssetNetworks
                .Where(an => !activeAssets.Contains(an.Asset))
                .ToListAsync(cancellationToken);

            if (assetNetworksToRemove.Any())
            {
                _dbContext.AssetNetworks.RemoveRange(assetNetworksToRemove);
                await _dbContext.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Removed {Count} asset networks from assets not in the selected list", assetNetworksToRemove.Count);
            }

            var entities = new List<AssetNetworkEntity>();
            var successCount = 0;
            var errorCount = 0;

            foreach (var asset in activeAssets)
            {
                try
                {
                    _logger.LogInformation("Collecting networks for asset: {Asset}", asset);
                    var startTime = DateTime.UtcNow;
                    dynamic networksResponse = await _mercadoBitcoinClient.GetAssetNetworksAsync(asset);
                    var endTime = DateTime.UtcNow;
                    _logger.LogInformation("API call for {Asset} networks took {Duration}ms", asset, (endTime - startTime).TotalMilliseconds);

                    if (networksResponse == null)
                    {
                        _logger.LogWarning("Null response for networks of asset {Asset}", asset);
                        errorCount++;
                        continue;
                    }
                    _logger.LogInformation("Raw networks response for {Asset}: {Response}", asset, JsonSerializer.Serialize((object)networksResponse));

                    IEnumerable<dynamic> list;
                    if (networksResponse is IEnumerable<dynamic> enumerable)
                        list = enumerable;
                    else
                        list = new List<dynamic> { networksResponse };

                    int localCount = 0;
                    foreach (var network in list)
                    {
                        try
                        {
                            string networkName = TryGetString(() => network.network) ?? TryGetString(() => network.Network) ?? "UNKNOWN";
                            bool isDefault = TryGetBool(() => network.is_default) || TryGetBool(() => network.IsDefault);
                            bool withdrawalEnabled = TryGetBool(() => network.withdrawal_enabled) || TryGetBool(() => network.WithdrawalEnabled);
                            decimal withdrawalFee = TryGetDecimal(() => network.withdrawal_fee) ?? TryGetDecimal(() => network.WithdrawalFee) ?? 0m;
                            decimal minWithdrawal = TryGetDecimal(() => network.min_withdrawal_amount) ?? TryGetDecimal(() => network.MinWithdrawalAmount) ?? 0m;

                            entities.Add(new AssetNetworkEntity
                            {
                                Asset = asset,
                                Network = networkName,
                                IsDefault = isDefault,
                                WithdrawalFee = withdrawalFee,
                                MinWithdrawalAmount = minWithdrawal,
                                WithdrawalEnabled = withdrawalEnabled,
                                CollectedAt = DateTime.UtcNow
                            });
                            localCount++;
                        }
                        catch (Exception mapEx)
                        {
                            _logger.LogDebug(mapEx, "Failed to map network item for {Asset}", asset);
                        }
                    }
                    _logger.LogInformation("Mapped {Count} networks for {Asset}", localCount, asset);
                    successCount++;
                }
                catch (Exception ex)
                {
                    errorCount++;
                    _logger.LogWarning(ex, "Error collecting asset networks for {Asset} ({Error}/{Total})", asset, errorCount, activeAssets.Count);
                }
            }

            _logger.LogInformation("Asset networks collection completed: {Success} successful, {Error} errors out of {Total} assets", successCount, errorCount, activeAssets.Count);

            if (entities.Any())
            {
                _logger.LogInformation("Attempting to save {Count} asset networks to database", entities.Count);

                await _dbContext.AssetNetworks.AddRangeAsync(entities, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Saved {Count} asset networks to database", entities.Count);
            }
            else
            {
                _logger.LogWarning("No asset networks were collected successfully");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting asset networks");
            throw;
        }
    }

    public async Task<List<string>> GetSymbolsAsync()
    {
        return await _dbContext.Symbols
            .Select(s => s.Symbol)
            .ToListAsync();
    }

    private static decimal ParseDecimal(dynamic value)
    {
        if (value == null) return 0;
        if (value is decimal d) return d;
        if (value is int i) return (decimal)i;
        if (value is long l) return (decimal)l;
        if (value is double db) return (decimal)db;
        if (value is float f) return (decimal)f;
        return decimal.Parse(value.ToString());
    }

    private static long ParseLong(dynamic value)
    {
        if (value == null) return 0;
        if (value is long l) return l;
        if (value is int i) return (long)i;
        if (value is decimal d) return (long)d;
        if (value is double db) return (long)db;
        if (value is float f) return (long)f;
        return long.Parse(value.ToString());
    }

    private long SafeToLong(dynamic value)
    {
        try
        {
            if (value == null) return 0L;
            if (value is long l) return l;
            if (value is int i) return i;
            if (value is string s && long.TryParse(s, out long parsed)) return parsed;
            if (long.TryParse(Convert.ToString(value), out long gen)) return gen;
            return 0L;
        }
        catch { return 0L; }
    }

    private string? TryGetString(Func<dynamic> getter)
    {
        try { var v = getter(); return v?.ToString(); } catch { return null; }
    }
    private bool TryGetBool(Func<dynamic> getter)
    {
        try { var v = getter(); if (v is bool b) return b; if (bool.TryParse(Convert.ToString(v), out bool parsed)) return parsed; } catch { }
        return false;
    }
    private decimal? TryGetDecimal(Func<dynamic> getter)
    {
        try { var v = getter(); if (v == null) return null; return ParseDecimal(v); } catch { return null; }
    }
}
