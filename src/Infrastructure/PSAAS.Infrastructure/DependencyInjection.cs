using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PSAAS.Infrastructure.Data.DbContexts;

namespace PSAAS.Infrastructure;

public static class DependencyInjection
{
    private const string PsaasDbConnectionStringName = "psaas_db";

    public static IServiceCollection AddPsaasInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(PsaasDbConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Missing required connection string 'ConnectionStrings:{PsaasDbConnectionStringName}'.");

        services.AddDbContext<PsaasDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
        });

        return services;
    }
}
