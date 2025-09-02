using Dados.Data;
using Dados.Services;
using MercadoBitcoin.Client;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Add Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Dados API - CryptoTrading v5.0",
        Version = "v1",
        Description = "API para coleta e persistência de dados públicos do Mercado Bitcoin",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "CryptoTrading Team",
            Email = "support@cryptotrading.com"
        }
    });
});

// Add DbContext
builder.Services.AddDbContext<CryptoTradingDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add services
builder.Services.AddScoped<DataIngestionService>();
builder.Services.AddHttpClient();
builder.Services.AddScoped<MercadoBitcoinClient>();

var app = builder.Build();

// Apply migrations on startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<CryptoTradingDbContext>();
    await dbContext.Database.MigrateAsync();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Dados API v1");
        c.RoutePrefix = "swagger";
        c.DocumentTitle = "Dados API - CryptoTrading v5.0";
        c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
        c.DefaultModelsExpandDepth(-1);
    });
}
else
{
    // Enable Swagger in production for containerized environments
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Dados API v1");
        c.RoutePrefix = "swagger";
        c.DocumentTitle = "Dados API - CryptoTrading v5.0";
        c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
        c.DefaultModelsExpandDepth(-1);
    });
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();

public partial class Program { }
