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
        var dadosUrl = isDevelopment ? "http://localhost:5001" : "http://ct_dados:8080";
        var sugestoesUrl = isDevelopment ? "http://localhost:5002" : "http://ct_sugestoes:8080";
        var negociacoesUrl = isDevelopment ? "http://localhost:5003" : "http://ct_negociacoes:8080";

        var agendamentos = new List<Agendamento>
        {

        };

        await context.Agendamentos.AddRangeAsync(agendamentos);
        await context.SaveChangesAsync();

        Console.WriteLine($"Inseridos {agendamentos.Count} agendamentos na base de dados.");
    }
}
