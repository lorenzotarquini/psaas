using NLog;
using NLog.Extensions.Logging;
using NLog.Web;
using PSAAS.Application;
using PSAAS.AdminPortal.Backend.Infrastructure;
using PSAAS.AdminPortal.Backend.Jobs;
using PSAAS.Application.Abstractions.Audit;
using PSAAS.Application.Abstractions.Tenants;
using PSAAS.Application.Abstractions.Users;
using PSAAS.Auth;
using PSAAS.Infrastructure;
using PSAAS.Infrastructure.Users;

var logger = LogManager.Setup()
    .LoadConfigurationFromAppSettings()
    .GetCurrentClassLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // The early load above only knows the process environment; reload from the host configuration
    // so environment files (appsettings.Development.json) and other sources apply as well.
    LogManager.Configuration = new NLogLoggingConfiguration(builder.Configuration.GetSection("NLog"));
    builder.Logging.ClearProviders();
    builder.Host.UseNLog();

    builder.Services.AddKeycloak(builder.Configuration);
    builder.Services.AddPsaasApplication();
    builder.Services.AddPsaasInfrastructure(builder.Configuration);
    builder.Services.AddScoped<ICurrentActor, HttpCurrentActor>();
    builder.Services.AddHostedService<ProvisioningRetryJob>();
    // UC6/UC7: users live in Keycloak, reached through its admin API with this client's service account
    // (Keycloak:ClientSecret, roles view-users and manage-users of realm-management).
    builder.Services.AddTransient<KeycloakClientCredentialsHandler>();
    builder.Services.AddHttpClient<IUserDirectory, KeycloakUserDirectory>(http =>
        {
            var authority = builder.Configuration["Keycloak:Authority"]!.TrimEnd('/');
            http.BaseAddress = new Uri(authority.Replace("/realms/", "/admin/realms/") + "/");
            http.Timeout = TimeSpan.FromSeconds(10);
        })
        .AddHttpMessageHandler<KeycloakClientCredentialsHandler>();
    if (string.IsNullOrWhiteSpace(builder.Configuration["Keycloak:ClientSecret"]))
    {
        // Not fatal: the rest of the portal works, the users pages answer 503 until it is set.
        logger.Warn("Keycloak:ClientSecret is not configured: users (UC6, UC7) are unavailable");
    }
    builder.Services.AddControllers();
    builder.Services.AddProblemDetails();
    builder.Services.AddHealthChecks();
    builder.Services.AddOpenApi();
    builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
        .WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
        .AllowAnyHeader()
        .AllowAnyMethod()));

    var app = builder.Build();

    app.UseExceptionHandler(new ExceptionHandlerOptions
    {
        StatusCodeSelector = exception => exception switch
        {
            TenantUniquenessConflictException or ProductiveUnitNameConflictException => StatusCodes.Status409Conflict,
            UserDirectoryUnavailableException => StatusCodes.Status503ServiceUnavailable,
            _ => StatusCodes.Status500InternalServerError
        }
    });
    app.UseStatusCodePages();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

    app.UseHttpsRedirection();
    app.UseCors();
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapHealthChecks("/health");

    // Every controller requires the Administrator policy unless it opts out explicitly.
    app.MapControllers().RequireAuthorization("Administrator");

    app.Run();
}
catch (Exception exception) when (exception is not HostAbortedException)
{
    logger.Error(exception, "Application stopped because of an unhandled exception.");
    throw;
}
finally
{
    LogManager.Shutdown();
}

public partial class Program;
