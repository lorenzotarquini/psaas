using System.Globalization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using NLog;
using NLog.Web;
using PSAAS.AdminPortal.Components;
using PSAAS.AdminPortal.Features.Tenants.Services;
using PSAAS.AdminPortal.Features.Tenants.ViewModels;
using PSAAS.Auth;
using PSAAS.Infrastructure;
using PSAAS.Infrastructure.Data.DbContexts;

var logger = LogManager.Setup()
    .LoadConfigurationFromAppSettings()
    .GetCurrentClassLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Logging.ClearProviders();
    builder.Host.UseNLog();

    // Add services to the container.
    builder.Services.AddKeycloak(builder.Configuration);
    builder.Services.AddPsaasInfrastructure(builder.Configuration);
    builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
    builder.Services.AddMudServices();
    builder.Services.AddScoped<ITenantService, TenantService>();
    builder.Services.AddScoped<ITenantCreateService, TenantCreateService>();
    builder.Services.AddScoped<ITenantEditService, TenantEditService>();
    builder.Services.AddTransient<TenantCreateFormViewModel>();
    builder.Services.AddTransient<TenantEditFormViewModel>();
    builder.Services.AddTransient<TenantGridViewModel>();
    builder.Services.AddCascadingAuthenticationState();
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor
            | ForwardedHeaders.XForwardedHost
            | ForwardedHeaders.XForwardedProto;
    });

    builder.Services.AddRazorComponents()
        .AddInteractiveServerComponents();

    var supportedCultures = new[]
    {
        new CultureInfo("it-IT"),
        new CultureInfo("en-US")
    };

    builder.Services.Configure<RequestLocalizationOptions>(options =>
    {
        options.DefaultRequestCulture = new RequestCulture("it-IT");
        options.SupportedCultures = supportedCultures;
        options.SupportedUICultures = supportedCultures;
    });

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error", createScopeForErrors: true);
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
    }
    app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
    app.UseForwardedHeaders();
    app.UseRequestLocalization();
    app.UseHttpsRedirection();

    app.UseAuthentication();
    app.UseAuthorization();
    app.UseAntiforgery();

    app.MapKeycloakLogout();

    app.MapStaticAssets();
    app.MapRazorComponents<App>()
        .AddInteractiveServerRenderMode()
        // The endpoint requires an authenticated user so unauthenticated requests use
        // the PSAAS.Auth OIDC challenge. The Administrator role is enforced by
        // AuthorizeRouteView through the component-level policy, allowing authenticated
        // users without the role to see the in-app access denied message instead of a
        // cookie access-denied redirect to a non-existent placeholder endpoint.
        .RequireAuthorization();

    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<PsaasDbContext>();
        dbContext.Database.Migrate();
    }

    app.Run();
}
catch (Exception exception)
{
    logger.Error(exception, "Application stopped because of an unhandled exception.");
    throw;
}
finally
{
    LogManager.Shutdown();
}
