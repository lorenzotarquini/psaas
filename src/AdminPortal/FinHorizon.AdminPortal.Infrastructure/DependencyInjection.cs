using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using FinHorizon.AdminPortal.Application.Abstractions.Audit;
using FinHorizon.AdminPortal.Application.Abstractions.Catalog;
using FinHorizon.AdminPortal.Application.Abstractions.Geography;
using FinHorizon.AdminPortal.Application.Abstractions.Profiles;
using FinHorizon.AdminPortal.Application.Abstractions.Tenants;
using FinHorizon.AdminPortal.Infrastructure.Audit;
using FinHorizon.AdminPortal.Infrastructure.Catalog;
using FinHorizon.AdminPortal.Infrastructure.Data.DbContexts;
using FinHorizon.AdminPortal.Infrastructure.Geography;
using FinHorizon.AdminPortal.Infrastructure.Repositories.Profiles;
using FinHorizon.AdminPortal.Infrastructure.Repositories.Tenants;

namespace FinHorizon.AdminPortal.Infrastructure;

public static class DependencyInjection
{
    private const string AdminPortalDbConnectionStringName = "adminportal-db";

    public static IServiceCollection AddAdminPortalInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(AdminPortalDbConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Missing required connection string 'ConnectionStrings:{AdminPortalDbConnectionStringName}'.");

        services.AddDbContext<AdminPortalDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
        });

        services.AddScoped<ITenantReadRepository, TenantReadRepository>();
        services.AddScoped<ITenantListRepository, TenantListRepository>();
        services.AddScoped<FinHorizon.AdminPortal.Application.Abstractions.Billing.IInvoiceRepository, Billing.InvoiceRepository>();
        services.AddSingleton<FinHorizon.AdminPortal.Application.Abstractions.Billing.IInvoiceTransmitter, Billing.NoOpInvoiceTransmitter>();
        services.AddScoped<FinHorizon.AdminPortal.Application.Abstractions.Usage.IUsageRepository, Usage.UsageRepository>();
        services.AddScoped<FinHorizon.AdminPortal.Application.Abstractions.Communications.ICommunicationRepository, Communications.CommunicationRepository>();
        services.AddSingleton<FinHorizon.AdminPortal.Application.Abstractions.Communications.IEmailSender, Communications.SmtpEmailSender>();
        services.AddScoped<FinHorizon.AdminPortal.Application.Abstractions.Health.IServiceProbe, Communications.EmailProbe>();
        services.AddScoped<FinHorizon.AdminPortal.Application.Abstractions.Health.IServiceProbe, Usage.DatabaseProbe>();
        services.AddScoped<FinHorizon.AdminPortal.Application.Abstractions.Health.IServiceProbe, Usage.InvoiceTransmissionProbe>();
        services.AddScoped<FinHorizon.AdminPortal.Application.Abstractions.Views.ISavedViewRepository, Repositories.Views.SavedViewRepository>();
        services.AddScoped<ITenantWriteRepository, TenantWriteRepository>();
        services.AddScoped<ITenantUniquenessChecker, TenantUniquenessChecker>();
        services.AddScoped<IProfileReadRepository, ProfileReadRepository>();
        services.AddScoped<AuditLog>();
        services.AddScoped<IAuditLog>(sp => sp.GetRequiredService<AuditLog>());
        services.AddScoped<IAuditReadRepository>(sp => sp.GetRequiredService<AuditLog>());
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<CatalogRepository>();
        services.AddScoped<ICatalogRepository>(sp => sp.GetRequiredService<CatalogRepository>());
        services.AddScoped<ISubscriptionRepository>(sp => sp.GetRequiredService<CatalogRepository>());
        // Singleton HttpClient like the city list: module hooks get 10 seconds.
        services.AddSingleton<IModuleProvisioner>(_ => new HttpModuleProvisioner(new HttpClient { Timeout = TimeSpan.FromSeconds(10) }));

        services.AddMemoryCache();
        // Singleton HttpClient: avoids the Microsoft.Extensions.Http package.
        services.AddSingleton<IItalianCityProvider>(sp => new ItalianCityProvider(
            new HttpClient
            {
                BaseAddress = new Uri(configuration["ItalianCities:BaseUrl"]
                    ?? throw new InvalidOperationException("Missing required setting 'ItalianCities:BaseUrl'.")),
                Timeout = TimeSpan.FromSeconds(10),
            },
            sp.GetRequiredService<Microsoft.Extensions.Caching.Memory.IMemoryCache>()));

        return services;
    }
}
