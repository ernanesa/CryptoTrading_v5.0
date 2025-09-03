using Dados.Controllers;
using Dados.Data;
using Dados.Services;
using MercadoBitcoin.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Dados API",
        Version = "v1",
        Description = "API para coleta e persistência de dados públicos do Mercado Bitcoin"
    });
});

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

// Registrar serviços
builder.Services.AddScoped<DataIngestionService>();
builder.Services.AddScoped<MercadoBitcoinClient>();

builder.Services.AddDbContext<CryptoTradingDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"), npgsqlOptions =>
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

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Modo Native AOT: evitar EF migrations dinâmicas. Garantir tabelas críticas via SQL idempotente mínimo.
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<CryptoTradingDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        logger.LogInformation("🔧 Verificando tabelas essenciais (modo AOT sem migrations)...");
        // Exemplo: garantir tabela Symbols (demais tabelas assumidas criadas via init.sql ou script externo)
        var ensureSymbols = @"CREATE TABLE IF NOT EXISTS ""Symbols"" (
    ""Symbol"" text PRIMARY KEY,
    ""BaseCurrency"" text NULL,
    ""QuoteCurrency"" text NULL,
    ""Status"" text NULL,
    ""BasePrecision"" int NULL,
    ""QuotePrecision"" int NULL,
    ""AmountPrecision"" int NULL,
    ""MinOrderAmount"" numeric NULL,
    ""MinOrderValue"" numeric NULL,
    ""CollectedAt"" timestamp without time zone NULL
);";
        context.Database.ExecuteSqlRaw(ensureSymbols);
        logger.LogInformation("✅ Tabela Symbols verificada");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Erro ao garantir tabelas mínimas");
        throw;
    }
}

app.Services.GetRequiredService<ILogger<Program>>().LogInformation("🚀 Serviço Dados iniciado com sucesso");
app.Run();

public partial class Program { }
