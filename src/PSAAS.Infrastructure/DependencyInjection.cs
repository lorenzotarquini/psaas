using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PSAAS.Application.Abstractions.Geography;
using PSAAS.Application.Abstractions.Profiles;
using PSAAS.Application.Abstractions.Tenants;
using PSAAS.Application.Abstractions.Users;
using PSAAS.Application.Validators.Tenants;
using PSAAS.Infrastructure.Data.DbContexts;
using PSAAS.Infrastructure.Geography;
using PSAAS.Infrastructure.Repositories.Profiles;
using PSAAS.Infrastructure.Repositories.Tenants;
using PSAAS.Infrastructure.Repositories.Users;

namespace PSAAS.Infrastructure;

public static class DependencyInjection
{
    private const string PsaasDbConnectionStringName = "psaas-db";

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

        services.AddScoped<ITenantReadRepository, TenantReadRepository>();
        services.AddScoped<ITenantWriteRepository, TenantWriteRepository>();
        services.AddScoped<ITenantUniquenessChecker, TenantUniquenessChecker>();
        services.AddScoped<IProfileReadRepository, ProfileReadRepository>();
        services.AddScoped<IUserReadRepository, UserReadRepository>();
        services.AddScoped<TenantCreateValidator>();

        services.AddMemoryCache();
        // Singleton HttpClient: avoids the Microsoft.Extensions.Http package.
        services.AddSingleton<IItalianCityProvider>(sp => new ItalianCityProvider(
            new HttpClient
            {
                BaseAddress = new Uri(configuration["ItalianCities:BaseUrl"]
                    ?? throw new InvalidOperationException("Missing required setting 'ItalianCities:BaseUrl'.")),
                Timeout = TimeSpan.FromSeconds(10),
            },
            sp.GetRequiredService<Microsoft.Extensions.Caching.Memory.IMemoryCache>(),
            sp.GetRequiredService<ILogger<ItalianCityProvider>>()));

        return services;
    }
}
