using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PSAAS.Application.Abstractions.Health;
using PSAAS.Application.Contracts.Usage;
using PSAAS.Application.Services.Health;
using PSAAS.Application.Services.Usage;

namespace PSAAS.AdminPortal.Backend.Controllers;

// UC25 step 2: the "Uso" tab of a tenant.
[ApiController]
public sealed class UsageController(UsageService usage) : ControllerBase
{
    [HttpGet("api/tenants/{tenantId:guid}/usage")]
    public async Task<ActionResult<TenantUsage>> Get(Guid tenantId, CancellationToken cancellationToken, [FromQuery] int days = 30)
    {
        if (days is < 1 or > 365)
        {
            return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]> { ["days"] = ["InvalidValue"] }));
        }

        return await usage.GetTenantUsageAsync(tenantId, days, cancellationToken) is { } result ? result : NotFound();
    }

    /// <summary>
    /// UC25 step 1, mapped in Program.cs outside the controllers: it takes the UsageReporter policy instead of
    /// Administrator (UC25-R2).
    /// </summary>
    public static async Task<IResult> ReportAsync(UsageReport report, UsageService usage, CancellationToken cancellationToken)
    {
        var errors = await usage.ReportAsync(report, cancellationToken);
        return errors.Count == 0
            ? Results.NoContent()
            : Results.ValidationProblem(errors.ToDictionary(e => e.Key, e => new[] { e.Value }));
    }
}

// UC26: connected systems at a glance.
[ApiController]
[Route("api/system")]
public sealed class SystemController(ServicesHealthService health) : ControllerBase
{
    [HttpGet("services")]
    public Task<ServicesHealth> GetServices(CancellationToken cancellationToken) => health.CheckAsync(cancellationToken);
}

/// <summary>UC26: Keycloak as identity provider, through the discovery document the backend validates tokens with.</summary>
public sealed class KeycloakSignInProbe(IOptionsMonitor<JwtBearerOptions> options) : IServiceProbe
{
    public string Key => "keycloak";

    public async Task<ProbeResult> CheckAsync(CancellationToken cancellationToken)
    {
        var jwt = options.Get(JwtBearerDefaults.AuthenticationScheme);
        // Static configuration (tests) needs no call; otherwise the cached document, refreshed when due.
        var configuration = jwt.Configuration
            ?? await (jwt.ConfigurationManager ?? throw new InvalidOperationException("Keycloak not configured")).GetConfigurationAsync(cancellationToken);
        return new ProbeResult(ServiceStatuses.Up, configuration.Issuer);
    }
}
