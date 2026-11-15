using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using FinHorizon.MySync.Infrastructure.Data;

namespace FinHorizon.MySync.Infrastructure;

public static class DependencyInjection
{
    private const string MySyncDbConnectionStringName = "mysync-db";

    public static IServiceCollection AddMySyncInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(MySyncDbConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Missing required connection string 'ConnectionStrings:{MySyncDbConnectionStringName}'.");

        services.AddDbContext<MySyncDbContext>(options => options.UseNpgsql(connectionString));
        services.AddSingleton(TimeProvider.System);
        return services;
    }
}
