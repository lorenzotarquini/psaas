using System.Text.Json.Serialization;
using NLog;
using NLog.Extensions.Logging;
using NLog.Web;
using FinHorizon.Auth;
using FinHorizon.MySync.Application;
using FinHorizon.MySync.Application.Abstractions;
using FinHorizon.MySync.Backend.Infrastructure;
using FinHorizon.MySync.Infrastructure;

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
    builder.Services.AddMySyncApplication();
    builder.Services.AddMySyncInfrastructure(builder.Configuration);
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();
    builder.Services.AddControllers()
        .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
    builder.Services.AddProblemDetails();
    builder.Services.AddExceptionHandler<CurrencyServiceExceptionHandler>();
    builder.Services.AddHealthChecks();
    builder.Services.AddOpenApi();
    builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
        .WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
        .AllowAnyHeader()
        .AllowAnyMethod()));

    var app = builder.Build();

    // Every project of the solution brings its database up to date at start.
    await app.Services.MigrateMySyncDatabaseAsync();

    app.UseExceptionHandler();
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

    // Every controller requires a MySync user (policy User: role module-mysync, given by the Admin Portal) unless it opts out explicitly.
    app.MapControllers().RequireAuthorization("User");

    await app.RunAsync();
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
