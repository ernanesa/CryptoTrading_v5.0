using Dados.Data;
using Dados.Entities;
using MercadoBitcoin.Client;
using MercadoBitcoin.Client.Extensions;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

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
            // Usar lista fixa das 50 melhores criptomoedas
            var activeSymbols = GetTopTier50CryptocurrencyList();

            if (!activeSymbols.Any())
            {
                _logger.LogWarning("No symbols found in the predefined list");
                return;
            }

            // Limpar tickers de símbolos que não estão na lista selecionada
            var tickersToRemove = await _dbContext.Tickers
                .Where(t => !activeSymbols.Contains(t.Symbol))
                .ToListAsync(cancellationToken);

            if (tickersToRemove.Any())
            {
                _dbContext.Tickers.RemoveRange(tickersToRemove);
                _logger.LogInformation("Removed {Count} tickers from symbols not in the selected list", tickersToRemove.Count);
            }

            var entities = new List<TickerEntity>();

            // Processar um símbolo por vez para evitar problemas
            foreach (var symbol in activeSymbols)
            {
                try
                {
                    _logger.LogInformation("Processing symbol: {Symbol}", symbol);
                    dynamic tickerResponse = await _mercadoBitcoinClient.GetTickersAsync(symbol);

                    // var responseJson = JsonSerializer.Serialize(tickerResponse);
                    // ((ILogger)_logger).LogInformation("Response for {Symbol}: {Response}", symbol, responseJson);

                    // Assumir que a resposta é um único objeto ticker
                    var entity = new TickerEntity
                    {
                        Symbol = symbol,
                        Last = ParseDecimal(tickerResponse?.Last),
                        High = ParseDecimal(tickerResponse?.High),
                        Low = ParseDecimal(tickerResponse?.Low),
                        Vol = ParseDecimal(tickerResponse?.Vol),
                        Buy = ParseDecimal(tickerResponse?.Buy),
                        Sell = ParseDecimal(tickerResponse?.Sell),
                        Date = ParseLong(tickerResponse?.Date),
                        CollectedAt = DateTime.UtcNow
                    };

                    entities.Add(entity);
                    _logger.LogInformation("Successfully processed ticker for {Symbol}", symbol);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error collecting ticker for symbol {Symbol}", symbol);
                    // Continue com outros símbolos
                }
            }

            if (entities.Any())
            {
                await _dbContext.Tickers.AddRangeAsync(entities, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Collected {Count} tickers for {SymbolCount} predefined symbols", entities.Count, activeSymbols.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting tickers");
            throw;
        }
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

            // Limpar order books de símbolos que não estão na lista selecionada
            var orderBooksToRemove = await _dbContext.OrderBooks
                .Where(ob => !activeSymbols.Contains(ob.Symbol))
                .ToListAsync(cancellationToken);

            if (orderBooksToRemove.Any())
            {
                _dbContext.OrderBooks.RemoveRange(orderBooksToRemove);
                _logger.LogInformation("Removed {Count} order books from symbols not in the selected list", orderBooksToRemove.Count);
            }

            var entities = new List<OrderBookEntity>();

            foreach (var symbol in activeSymbols)
            {
                try
                {
                    dynamic orderBookResponse = await _mercadoBitcoinClient.GetOrderBookAsync(symbol, limit);
                    var entity = new OrderBookEntity
                    {
                        Symbol = symbol,
                        Bids = JsonSerializer.Serialize(orderBookResponse.Bids),
                        Asks = JsonSerializer.Serialize(orderBookResponse.Asks),
                        CollectedAt = DateTime.UtcNow
                    };
                    entities.Add(entity);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error collecting order book for symbol {Symbol}", symbol);
                    // Continue com outros símbolos
                }
            }

            if (entities.Any())
            {
                await _dbContext.OrderBooks.AddRangeAsync(entities, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Collected order books for {Count} symbols", entities.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting order books");
            throw;
        }
    }

    public async Task CollectTradesAsync(int? limit = null, CancellationToken cancellationToken = default)
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

            // Limpar trades de símbolos que não estão na lista selecionada
            var tradesToRemove = await _dbContext.Trades
                .Where(t => !activeSymbols.Contains(t.Symbol))
                .ToListAsync(cancellationToken);

            if (tradesToRemove.Any())
            {
                _dbContext.Trades.RemoveRange(tradesToRemove);
                _logger.LogInformation("Removed {Count} trades from symbols not in the selected list", tradesToRemove.Count);
            }

            var allEntities = new List<TradeEntity>();

            foreach (var symbol in activeSymbols)
            {
                try
                {
                    dynamic tradesResponse = await _mercadoBitcoinClient.GetTradesAsync(symbol, null, null, null, null, limit);
                    var entities = ((IEnumerable<dynamic>)tradesResponse).Select(t => new TradeEntity
                    {
                        Symbol = symbol,
                        Tid = (int)t.Tid,
                        Date = long.Parse((string)t.Date),
                        Price = decimal.Parse((string)t.Price),
                        Type = (string)t.Type,
                        Amount = decimal.Parse((string)t.Amount),
                        CollectedAt = DateTime.UtcNow
                    });
                    allEntities.AddRange(entities);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error collecting trades for symbol {Symbol}", symbol);
                    // Continue com outros símbolos
                }
            }

            if (allEntities.Any())
            {
                await _dbContext.Trades.AddRangeAsync(allEntities, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Collected {Count} trades for {SymbolCount} symbols", allEntities.Count, activeSymbols.Count);
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
            // Usar lista fixa das 50 melhores criptomoedas
            var activeSymbols = GetTopTier50CryptocurrencyList();

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
                _logger.LogInformation("Removed {Count} candles from symbols not in the selected list", candlesToRemove.Count);
            }

            var to = (int)DateTimeOffset.Now.ToUnixTimeSeconds();
            var allEntities = new List<CandleEntity>();

            foreach (var symbol in activeSymbols)
            {
                try
                {
                    dynamic candlesResponse = await _mercadoBitcoinClient.GetCandlesAsync(symbol, resolution, to, null, countback);
                    var entities = ((IEnumerable<dynamic>)candlesResponse).Select(c => new CandleEntity
                    {
                        Symbol = symbol,
                        Resolution = resolution,
                        Timestamp = long.Parse((string)c.Timestamp),
                        Open = decimal.Parse((string)c.Open),
                        High = decimal.Parse((string)c.High),
                        Low = decimal.Parse((string)c.Low),
                        Close = decimal.Parse((string)c.Close),
                        Volume = decimal.Parse((string)c.Volume),
                        CollectedAt = DateTime.UtcNow
                    });
                    allEntities.AddRange(entities);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error collecting candles for symbol {Symbol}", symbol);
                    // Continue com outros símbolos
                }
            }

            if (allEntities.Any())
            {
                await _dbContext.Candles.AddRangeAsync(allEntities, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Collected {Count} candles for {SymbolCount} symbols with resolution {Resolution}", allEntities.Count, activeSymbols.Count, resolution);
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
            // Usar lista fixa das 50 melhores criptomoedas e extrair ativos únicos
            var symbols = GetTopTier50CryptocurrencyList();
            var activeAssets = GetUniqueAssetsFromSymbols(symbols);

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
                _logger.LogInformation("Removed {Count} asset networks from assets not in the selected list", assetNetworksToRemove.Count);
            }

            var entities = new List<AssetNetworkEntity>();

            foreach (var asset in activeAssets)
            {
                try
                {
                    dynamic networksResponse = await _mercadoBitcoinClient.GetAssetNetworksAsync(asset);

                    // Assumir que networksResponse é uma lista de redes
                    if (networksResponse is IEnumerable<dynamic> networks)
                    {
                        foreach (var network in networks)
                        {
                            var entity = new AssetNetworkEntity
                            {
                                Asset = asset,
                                Network = network.Network?.ToString() ?? "UNKNOWN",
                                IsDefault = network.IsDefault == true,
                                WithdrawalFee = ParseDecimal(network.WithdrawalFee ?? network.Fee),
                                MinWithdrawalAmount = ParseDecimal(network.MinWithdrawalAmount ?? network.MinAmount),
                                WithdrawalEnabled = network.WithdrawalEnabled == true,
                                CollectedAt = DateTime.UtcNow
                            };
                            entities.Add(entity);
                        }
                    }
                    else
                    {
                        // Se não for uma lista, tentar processar como um único objeto
                        var entity = new AssetNetworkEntity
                        {
                            Asset = asset,
                            Network = networksResponse.Network?.ToString() ?? "UNKNOWN",
                            IsDefault = networksResponse.IsDefault == true,
                            WithdrawalFee = ParseDecimal(networksResponse.WithdrawalFee ?? networksResponse.Fee),
                            MinWithdrawalAmount = ParseDecimal(networksResponse.MinWithdrawalAmount ?? networksResponse.MinAmount),
                            WithdrawalEnabled = networksResponse.WithdrawalEnabled == true,
                            CollectedAt = DateTime.UtcNow
                        };
                        entities.Add(entity);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error collecting asset networks for {Asset}", asset);
                    // Continue com outros ativos
                }
            }

            if (entities.Any())
            {
                await _dbContext.AssetNetworks.AddRangeAsync(entities, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Collected asset networks for {Count} assets", entities.Count);
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
}
