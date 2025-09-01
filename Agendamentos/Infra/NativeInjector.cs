using Agendamentos.Data;
using Microsoft.EntityFrameworkCore;

namespace Agendamentos.Infra
{
    public class NativeInjector
    {
        public static void RegisterServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<CryptoTradingDbContext>(options =>
                options.UseNpgsql(configuration.GetConnectionString("CryptoTradingDB")));
        }
    }
}
