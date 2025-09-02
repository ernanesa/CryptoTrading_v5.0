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

// Execute migrations and seed
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<CryptoTradingDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var environment = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();

    try
    {
        // Check if migrations table exists
        var migrationsTableExists = context.Database.ExecuteSqlRaw(
            "SELECT 1 FROM information_schema.tables WHERE table_name = '__EFMigrationsHistory'") > 0;

        if (migrationsTableExists)
        {
            logger.LogInformation("🔧 Database already initialized via init.sql, skipping migrations");
        }
        else
        {
            logger.LogInformation("🔧 Executando migrações para o serviço Dados...");
            context.Database.Migrate();
            logger.LogInformation("✅ Migrações concluídas para o serviço Dados");
        }
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Could not check migrations table, attempting migration anyway");
        context.Database.Migrate();
    }

    // Then seed data
    logger.LogInformation("🌱 Aplicando dados iniciais...");
    // Note: Dados service doesn't have seed data like Agendamentos
    logger.LogInformation("✅ Serviço Dados pronto");
}

app.Services.GetRequiredService<ILogger<Program>>().LogInformation("🚀 Serviço Dados iniciado com sucesso");
app.Run();

public partial class Program { }
