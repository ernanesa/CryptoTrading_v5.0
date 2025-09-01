using System.Globalization;
using Agendamentos;
using Agendamentos.Data;
using Microsoft.EntityFrameworkCore;

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
});

var host = builder.Build();

// Execute migrations and seed
using (var scope = host.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<CryptoTradingDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var environment = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();

    // Execute migrations first
    logger.LogInformation("🔧 Executando migrações para o serviço Agendamentos...");
    context.Database.Migrate();
    logger.LogInformation("✅ Migrações concluídas para o serviço Agendamentos");

    // Then seed data
    logger.LogInformation("🌱 Aplicando dados iniciais...");
    var isDevelopment = environment.IsDevelopment();
    await SeedAgendamentos.SeedAsync(context, isDevelopment);
    logger.LogInformation("✅ Dados iniciais aplicados com sucesso");
}

host.Services.GetRequiredService<ILogger<Program>>().LogInformation("🚀 Serviço Agendamentos iniciado com sucesso");
host.Run();
