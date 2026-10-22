using Microsoft.Extensions.DependencyInjection;
using PSAAS.Application.Services.Tenants;
using PSAAS.Application.Services.Users;
using PSAAS.Application.Validators.Tenants;

namespace PSAAS.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddPsaasApplication(this IServiceCollection services)
    {
        services.AddScoped<TenantCreateValidator>();
        services.AddScoped<TenantAddressValidator>();
        services.AddScoped<TenantCreateService>();
        services.AddScoped<TenantRegistryUpdateService>();
        services.AddScoped<TenantOrganizationService>();
        services.AddScoped<UserCreateService>();
        services.AddScoped<UserSearchService>();

        return services;
    }
}
