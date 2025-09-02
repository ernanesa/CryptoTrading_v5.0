# Microsserviço Dados - CryptoTrading v5.0

Este microsserviço é responsável por coletar e persistir dados públicos da API do Mercado Bitcoin de forma eficiente, utilizando PostgreSQL 17 como banco de dados.

## 📊 Lista de Criptomoedas

O serviço trabalha com uma **lista selecionada de 50 criptomoedas** organizadas por tiers de qualidade e liquidez:

### 🏆 **TIER 1: BLUE CHIPS** (Alta liquidez, baixo risco)
- BTC-BRL, ETH-BRL, SOL-BRL, ADA-BRL, AVAX-BRL
- MATIC-BRL, DOT-BRL, BNB-BRL, ATOM-BRL, NEAR-BRL

### 🥈 **TIER 2: LARGE CAPS** (Boa liquidez, médio risco)
- LINK-BRL, UNI-BRL, LTC-BRL, XRP-BRL, TRX-BRL
- VET-BRL, FIL-BRL, THETA-BRL, ICP-BRL, BCH-BRL

### 🥉 **TIER 3: MID CAPS** (Trading ativo)
- AAVE-BRL, MKR-BRL, SNX-BRL, SAND-BRL, MANA-BRL
- COMP-BRL, CRV-BRL, SUSHI-BRL, ENJ-BRL, 1INCH-BRL

### 🔧 **TIER 4: DEFI & UTILITY** (Casos de uso reais)
- GRT-BRL, YFI-BRL, BAL-BRL, REN-BRL, ZRX-BRL
- OMG-BRL, LRC-BRL, STORJ-BRL, BAND-BRL, KNC-BRL

### 🎯 **TIER 5: ALTCOINS PROMISSORAS** (Higher risk/reward)
- FTM-BRL, ALGO-BRL, EGLD-BRL, HBAR-BRL, XTZ-BRL
- ONE-BRL, AR-BRL, KSM-BRL, WAVES-BRL, ZIL-BRL
- DOGE-BRL, SHIB-BRL, AXS-BRL

## Configuração com Docker

### Pré-requisitos
- Docker e Docker Compose instalados

### Executar com Docker Compose
```bash
# Na pasta do projeto Dados
docker-compose up --build
```

Isso irá:
1. Iniciar o container PostgreSQL com as credenciais configuradas
2. Construir e iniciar o microsserviço Dados
3. Aplicar as migrations automaticamente (se configurado)

### Serviços
- **postgres_service**: PostgreSQL 17 em Alpine Linux
- **dados_service**: Microsserviço .NET 9 otimizado

### Acesso
- API: http://localhost:8080
- PostgreSQL: localhost:5432 (externo ao container)

## Configuração Manual (sem Docker)

### Banco de Dados
1. Instale PostgreSQL 17.
2. Crie um banco de dados chamado `CryptoTrading_DB`.
3. Atualize a connection string em `appsettings.json` com suas credenciais.

### Aplicar Migrations
```bash
dotnet ef database update
```

### Executar o Serviço
```bash
dotnet run
```

## Endpoints

- `GET /api/data/collect-symbols` - Coleta símbolos
- `GET /api/data/collect-tickers` - Coleta tickers
- `GET /api/data/collect-orderbook?symbol=BTC-BRL&limit=10` - Coleta order book
## Endpoints

- `GET /api/data/collect-symbols` - Coleta símbolos
- `GET /api/data/collect-tickers` - Coleta tickers das 50 criptomoedas selecionadas
- `GET /api/data/collect-orderbook?limit=10` - Coleta order books das 50 criptomoedas selecionadas
- `GET /api/data/collect-trades?limit=100` - Coleta trades das 50 criptomoedas selecionadas
- `GET /api/data/collect-candles?resolution=1h&countback=24` - Coleta candles das 50 criptomoedas selecionadas
- `GET /api/data/collect-asset-fees` - Coleta taxas dos ativos das 50 criptomoedas selecionadas
- `GET /api/data/collect-asset-networks` - Coleta redes dos ativos das 50 criptomoedas selecionadas
- `GET /api/data/health` - Health check
- `GET /api/data/collect-candles?symbol=BTC-BRL&resolution=1h&to=1641081600&from=1640995200&countback=24` - Coleta candles
- `GET /api/data/collect-asset-fees?asset=BTC` - Coleta taxas de ativo
- `GET /api/data/collect-asset-networks?asset=BTC` - Coleta redes de ativo
- `GET /api/data/health` - Health check

## Tecnologias
- .NET 9
- Entity Framework Core 9
- Npgsql.EntityFrameworkCore.PostgreSQL 9.0.0
- MercadoBitcoin.Client 2.1.0
- PostgreSQL 17
- Docker & Docker Compose

## Otimizações Docker
- Imagens Alpine Linux para tamanho reduzido
- Multi-stage build para otimizar layers
- Non-root user para segurança
- Health checks para orquestração
- Volumes para persistência de dados
