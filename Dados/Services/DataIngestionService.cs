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
            "ZIL-BRL",    // Zilliqa - Sharding pioneer
            "DOGE-BRL",   // Dogecoin - Popular altcoin
            "SHIB-BRL",   // Shiba Inu - Popular altcoin
            "AXS-BRL"     // Axie Infinity - Gaming/NFT
        };
    }

    public async Task CollectSymbolsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            dynamic symbolsResponse = await _mercadoBitcoinClient.GetSymbolsAsync();

            // Log the response structure for debugging
            Console.WriteLine($"Symbols response type: {symbolsResponse.GetType()}");
            Console.WriteLine($"Symbols response: {JsonSerializer.Serialize(symbolsResponse)}");

            // Try different approaches to access the symbols data
            IEnumerable<dynamic> symbolsData = null;

            if (symbolsResponse is IEnumerable<dynamic> enumerable)
            {
                symbolsData = enumerable;
            }
            else if (symbolsResponse.Symbol is IEnumerable<dynamic> symbolArray)
            {
                symbolsData = symbolArray;
            }
            else
            {
                _logger.LogWarning("Unexpected response structure for symbols");
                return;
            }

            var entities = symbolsData.Select(s =>
            {
                // Handle different possible structures
                if (s is string symbolString)
                {
                    // If it's just a string, create a minimal entity
                    return new SymbolEntity
                    {
                        Symbol = symbolString,
                        BaseCurrency = symbolString.Replace("-BRL", ""),
                        QuoteCurrency = "BRL",
                        Status = "ACTIVE",
                        BasePrecision = 8,
                        QuotePrecision = 2,
                        AmountPrecision = 8,
                        MinOrderAmount = 0.00000001m,
                        MinOrderValue = 1.00m,
                        CollectedAt = DateTime.UtcNow
                    };
                }
                else
                {
                    // Try to access properties dynamically
                    return new SymbolEntity
                    {
                        Symbol = (string)(s.Symbol ?? s.Pair ?? s),
                        BaseCurrency = (string)(s.BaseCurrency ?? s.Base ?? ((string)(s.Symbol ?? s.Pair ?? s)).Replace("-BRL", "")),
                        QuoteCurrency = (string)(s.QuoteCurrency ?? s.Quote ?? "BRL"),
                        Status = (string)(s.Status ?? "ACTIVE"),
                        BasePrecision = (int)(s.BasePrecision ?? 8),
                        QuotePrecision = (int)(s.QuotePrecision ?? 2),
                        AmountPrecision = (int)(s.AmountPrecision ?? 8),
                        MinOrderAmount = (decimal)(s.MinOrderAmount ?? 0.00000001m),
                        MinOrderValue = (decimal)(s.MinOrderValue ?? 1.00m),
                        CollectedAt = DateTime.UtcNow
                    };
                }
            });

            await _dbContext.Symbols.AddRangeAsync(entities, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Collected {Count} symbols", entities.Count());
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

            var symbolsParam = string.Join(",", activeSymbols);
            dynamic tickersResponse = await _mercadoBitcoinClient.GetTickersAsync(symbolsParam);
            var entities = ((IEnumerable<dynamic>)tickersResponse).Select(t => new TickerEntity
            {
                Symbol = (string)t.Pair,
                Last = decimal.Parse((string)t.Last),
                High = decimal.Parse((string)t.High),
                Low = decimal.Parse((string)t.Low),
                Vol = decimal.Parse((string)t.Vol),
                Buy = decimal.Parse((string)t.Buy),
                Sell = decimal.Parse((string)t.Sell),
                Date = long.Parse((string)t.Date),
                CollectedAt = DateTime.UtcNow
            });

            await _dbContext.Tickers.AddRangeAsync(entities, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Collected {Count} tickers for {SymbolCount} predefined symbols", entities.Count(), activeSymbols.Count);
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

            var allEntities = new List<AssetNetworkEntity>();

            foreach (var asset in activeAssets)
            {
                try
                {
                    dynamic networksResponse = await _mercadoBitcoinClient.GetAssetNetworksAsync(asset);
                    var entities = ((IEnumerable<dynamic>)networksResponse).Select(n => new AssetNetworkEntity
                    {
                        Asset = asset,
                        Network = (string)n.Network,
                        IsDefault = (bool)n.Is_default,
                        WithdrawalFee = decimal.Parse((string)n.Withdrawal_fee),
                        MinWithdrawalAmount = decimal.Parse((string)n.Min_withdrawal_amount),
                        WithdrawalEnabled = (bool)n.Withdrawal_enabled,
                        CollectedAt = DateTime.UtcNow
                    });
                    allEntities.AddRange(entities);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error collecting asset networks for {Asset}", asset);
                    // Continue com outros ativos
                }
            }

            if (allEntities.Any())
            {
                await _dbContext.AssetNetworks.AddRangeAsync(allEntities, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Collected {Count} asset networks for {AssetCount} assets", allEntities.Count, activeAssets.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting asset networks");
            throw;
        }
    }
}
