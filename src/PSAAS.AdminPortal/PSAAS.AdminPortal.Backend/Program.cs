using System.Security.Claims;
using NLog;
using NLog.Web;
using PSAAS.Application.Abstractions.Tenants;
using PSAAS.Auth;
using PSAAS.Infrastructure;

var logger = LogManager.Setup()
    .LoadConfigurationFromAppSettings()
    .GetCurrentClassLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Logging.ClearProviders();
    builder.Host.UseNLog();

    builder.Services.AddKeycloak(builder.Configuration);
    builder.Services.AddPsaasInfrastructure(builder.Configuration);
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

    // Every API endpoint requires the Administrator policy unless it opts out explicitly.
    var api = app.MapGroup("/api").RequireAuthorization("Administrator");

    api.MapGet("/me", (ClaimsPrincipal user) => new
    {
        Name = user.Identity?.Name,
        Roles = user.FindAll(ClaimTypes.Role).Select(claim => claim.Value).Distinct()
    });

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
