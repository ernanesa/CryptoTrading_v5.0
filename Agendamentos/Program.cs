using System.Globalization;
using Agendamentos;
using Agendamentos.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

var builder = Host.CreateApplicationBuilder(args);

// Configuração de logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

if (builder.Environment.IsDevelopment())
{
    builder.Logging.SetMinimumLevel(LogLevel.Debug);
}
else
{
    builder.Logging.SetMinimumLevel(LogLevel.Warning);
}

builder.Services.AddHostedService<Worker>();

builder.Services.AddDbContext<CryptoTradingDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("CryptoTradingDB"), npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorCodesToAdd: null);
        npgsqlOptions.CommandTimeout(30);
    });
    options.EnableSensitiveDataLogging(false);
    options.EnableServiceProviderCaching();
    // Suppress the pending model changes warning since we initialize via init.sql
    options.ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
});

var host = builder.Build();

// Inicialização sem migrações dinâmicas (compatível com Native AOT)
using (var scope = host.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<CryptoTradingDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var environment = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();

    try
    {
        logger.LogInformation("🔧 Garantindo existência da tabela Agendamentos (modo AOT, sem migrations)...");
        var createTableSql = @"CREATE TABLE IF NOT EXISTS ""Agendamentos"" (
    ""Id"" SERIAL PRIMARY KEY,
    ""Cron"" TEXT NULL,
    ""Route"" TEXT NULL,
    ""IsActive"" BOOLEAN NOT NULL DEFAULT TRUE
);";
        context.Database.ExecuteSqlRaw(createTableSql);
        logger.LogInformation("✅ Tabela Agendamentos verificada/criada");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Erro ao garantir tabela Agendamentos");
        throw;
    }

    try
    {
        logger.LogInformation("🌱 Aplicando dados iniciais (seed)...");
        var isDevelopment = environment.IsDevelopment();
        await SeedAgendamentos.SeedAsync(context, isDevelopment);
        logger.LogInformation("✅ Seed concluído");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Erro durante seed inicial");
        throw;
    }
}

host.Services.GetRequiredService<ILogger<Program>>().LogInformation("🚀 Serviço Agendamentos iniciado com sucesso");
host.Run();
