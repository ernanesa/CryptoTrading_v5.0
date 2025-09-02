using Agendamentos.Data;
using Agendamentos.Entities;
using Microsoft.EntityFrameworkCore;

namespace Agendamentos;

public static class SeedAgendamentos
{
    public static async Task SeedAsync(CryptoTradingDbContext context, bool isDevelopment = false)
    {
        // // Verifica se já existem agendamentos
        if (await context.Agendamentos.AnyAsync())
        {
            Console.WriteLine("Agendamentos já existem na base de dados.");
            return;
        }

        // URLs baseadas no ambiente
        var dadosUrl = isDevelopment ? "http://localhost:8080" : "http://dados_service:8080";
        var sugestoesUrl = isDevelopment ? "http://localhost:5002" : "http://ct_sugestoes:8080";
        var negociacoesUrl = isDevelopment ? "http://localhost:5003" : "http://ct_negociacoes:8080";

        var agendamentos = new List<Agendamento>
        {
            // === COLETA DE SÍMBOLOS ===
            // Executa uma vez por dia às 00:00 (meia-noite)
            new Agendamento
            {
                Cron = "0 0 * * *",
                Route = $"{dadosUrl}/api/Data/collect-symbols",
                IsActive = true
            },

            // === COLETA DE TICKERS (PREÇOS ATUAIS) ===
            // Executa a cada 5 minutos (dados de preço mudam frequentemente)
            new Agendamento
            {
                Cron = "*/5 * * * *",
                Route = $"{dadosUrl}/api/Data/collect-tickers",
                IsActive = true
            },

            // === COLETA DE ORDER BOOKS ===
            // Executa a cada 10 minutos (livros de ofertas mudam moderadamente)
            new Agendamento
            {
                Cron = "*/10 * * * *",
                Route = $"{dadosUrl}/api/Data/collect-order-books?limit=10",
                IsActive = true
            },

            // === COLETA DE TRADES ===
            // Executa a cada 15 minutos (trades recentes são importantes)
            new Agendamento
            {
                Cron = "*/15 * * * *",
                Route = $"{dadosUrl}/api/Data/collect-trades?limit=100",
                IsActive = true
            },

            // === COLETA DE CANDLES ===
            // Executa a cada hora (dados históricos de candles)
            new Agendamento
            {
                Cron = "0 * * * *",
                Route = $"{dadosUrl}/api/Data/collect-candles?resolution=1h&countback=24",
                IsActive = true
            },

            // === COLETA DE TAXAS DE ATIVOS ===
            // Executa uma vez por dia às 01:00 (taxas mudam raramente)
            new Agendamento
            {
                Cron = "0 1 * * *",
                Route = $"{dadosUrl}/api/Data/collect-asset-fees",
                IsActive = true
            },

            // === COLETA DE REDES DE ATIVOS ===
            // Executa uma vez por dia às 02:00 (redes mudam raramente)
            new Agendamento
            {
                Cron = "0 2 * * *",
                Route = $"{dadosUrl}/api/Data/collect-asset-networks",
                IsActive = true
            },

            // === CANDLES ADICIONAIS - RESOLUÇÃO DE 5 MINUTOS ===
            // Executa a cada 5 minutos para dados mais granulares
            new Agendamento
            {
                Cron = "*/5 * * * *",
                Route = $"{dadosUrl}/api/Data/collect-candles?resolution=5m&countback=12",
                IsActive = true
            },

            // === CANDLES ADICIONAIS - RESOLUÇÃO DIÁRIA ===
            // Executa uma vez por dia às 03:00
            new Agendamento
            {
                Cron = "0 3 * * *",
                Route = $"{dadosUrl}/api/Data/collect-candles?resolution=1d&countback=30",
                IsActive = true
            },

            // === ORDER BOOKS COM LIMITE MAIOR ===
            // Executa a cada 30 minutos com limite maior para análise mais profunda
            new Agendamento
            {
                Cron = "*/30 * * * *",
                Route = $"{dadosUrl}/api/Data/collect-order-books?limit=50",
                IsActive = true
            },

            // === TRADES COM MAIOR VOLUME ===
            // Executa a cada hora com limite maior para análise histórica
            new Agendamento
            {
                Cron = "0 * * * *",
                Route = $"{dadosUrl}/api/Data/collect-trades?limit=500",
                IsActive = true
            }
        };

        await context.Agendamentos.AddRangeAsync(agendamentos);
        await context.SaveChangesAsync();

        Console.WriteLine($"Inseridos {agendamentos.Count} agendamentos na base de dados.");
    }
}
