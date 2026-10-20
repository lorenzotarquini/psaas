using NLog;
using NLog.Extensions.Logging;
using NLog.Web;
using PSAAS.Application;
using PSAAS.Application.Abstractions.Tenants;
using PSAAS.Auth;
using PSAAS.Infrastructure;

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
