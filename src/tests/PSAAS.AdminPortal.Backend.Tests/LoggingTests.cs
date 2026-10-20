using System.Net;
using System.Net.Http.Json;
using NLog;
using NLog.Config;
using NLog.Targets;
using PSAAS.Application.Abstractions.Geography;
using Xunit;

namespace PSAAS.AdminPortal.Backend.Tests;

// NLog configuration is process-wide and every host shuts it down on dispose: keep these tests out of parallel runs.
[CollectionDefinition(nameof(LoggingTests), DisableParallelization = true)]
public sealed class LoggingCollection;

[Collection(nameof(LoggingTests))]
public sealed class LoggingTests
{
    [Fact]
    public async Task BackendApplicationAndRequestLogs_ReachNLog()
    {
        using var factory = new BackendFactory();
        factory.Cities.Result = [new ItalianCity("Roma", "RM", ["00118"])];
        var client = factory.CreateAdministratorClient();
        var memory = new MemoryTarget("memory") { Layout = "${logger}|${level}|${message}" };
        // First rule: the request-log rule is final and would stop messages before a rule added at the end.
        LogManager.Configuration!.LoggingRules.Insert(0, new LoggingRule("*", NLog.LogLevel.Trace, memory));
        LogManager.ReconfigExistingLoggers();

        var response = await client.PostAsJsonAsync("/api/tenants", new
        {
            companyName = "Acme Srl", completeVatNumber = "IT12345678901", certifiedEmail = "acme@pec.it",
            street = "Via Roma 1", city = "Roma", postalCode = "00118"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Contains(memory.Logs, log =>
            log.StartsWith("PSAAS.Application.Services.Tenants.TenantCreateService|Info|Created tenant", StringComparison.Ordinal));
        Assert.Contains(memory.Logs, log =>
            log.StartsWith("PSAAS.AdminPortal.Backend.Controllers.TenantsController|Info|Returned tenant", StringComparison.Ordinal));
        Assert.Contains(memory.Logs, log =>
            log.StartsWith("Microsoft.AspNetCore.Hosting.Diagnostics|Info|Request finished HTTP/1.1 POST", StringComparison.Ordinal));
    }

    [Fact]
    public void Development_LogsPsaasDebugRequestsAndOnlyOtherFrameworkWarnings()
    {
        using var factory = new BackendFactory();
        factory.CreateClient();

        Assert.True(LogManager.GetLogger("PSAAS.Infrastructure.Repositories.Tenants.TenantReadRepository").IsDebugEnabled);
        Assert.True(LogManager.GetLogger("Microsoft.AspNetCore.Hosting.Diagnostics").IsInfoEnabled);
        Assert.False(LogManager.GetLogger("Microsoft.AspNetCore.Routing.EndpointMiddleware").IsInfoEnabled);
        Assert.True(LogManager.GetLogger("Microsoft.AspNetCore.Routing.EndpointMiddleware").IsWarnEnabled);
    }
}
