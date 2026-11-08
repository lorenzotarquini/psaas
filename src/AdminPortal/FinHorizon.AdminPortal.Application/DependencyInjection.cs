using Microsoft.Extensions.DependencyInjection;
using FinHorizon.AdminPortal.Application.Services.Catalog;
using FinHorizon.AdminPortal.Application.Services.Tenants;
using FinHorizon.AdminPortal.Application.Services.Users;
using FinHorizon.AdminPortal.Application.Validators.Tenants;

namespace FinHorizon.AdminPortal.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddAdminPortalApplication(this IServiceCollection services)
    {
        services.AddScoped<TenantCreateValidator>();
        services.AddScoped<TenantAddressValidator>();
        services.AddScoped<TenantCreateService>();
        services.AddScoped<TenantRegistryUpdateService>();
        services.AddScoped<TenantOrganizationService>();
        services.AddScoped<ProfileService>();
        services.AddScoped<TenantLifecycleService>();
        services.AddScoped<CatalogService>();
        services.AddScoped<SubscriptionService>();
        services.AddScoped<UserCreateService>();
        services.AddScoped<UserSearchService>();
        services.AddScoped<UserRules>();
        services.AddScoped<TenantListService>();
        services.AddScoped<Services.Billing.BillingService>();
        services.AddScoped<Services.Usage.UsageService>();
        services.AddScoped<Services.Communications.CommunicationService>();
        services.AddScoped<Services.Communications.NotificationService>();
        services.AddScoped<Services.Overview.OverviewService>();
        services.AddScoped<Services.Overview.GlobalSearchService>();
        services.AddScoped<Services.Overview.ImpersonationService>();
        services.AddScoped<Services.Health.ServicesHealthService>();
        services.AddScoped<Abstractions.Health.IServiceProbe, Services.Health.UserDirectoryProbe>();
        services.AddScoped<Abstractions.Health.IServiceProbe, Services.Health.CityListProbe>();
        services.AddScoped<Abstractions.Health.IServiceProbe, Services.Health.ModuleHooksProbe>();
        services.AddSingleton(new Services.Billing.BillingOptions());
        services.AddScoped<Services.Views.SavedViewService>();
        services.AddScoped<UserManagementService>();
        services.AddScoped<UserImportService>();

        return services;
    }
}
