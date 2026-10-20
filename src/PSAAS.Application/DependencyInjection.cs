using Microsoft.Extensions.DependencyInjection;
using PSAAS.Application.Services.Tenants;
using PSAAS.Application.Validators.Tenants;

namespace PSAAS.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddPsaasApplication(this IServiceCollection services)
    {
        services.AddScoped<TenantCreateValidator>();
        services.AddScoped<TenantCreateService>();

        return services;
    }
}
