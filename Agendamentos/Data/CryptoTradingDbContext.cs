using Agendamentos.Entities;
using Microsoft.EntityFrameworkCore;

namespace Agendamentos.Data;

public class CryptoTradingDbContext(DbContextOptions<CryptoTradingDbContext> options) : DbContext(options)
{
    public DbSet<Agendamento> Agendamentos { get; set; }
}