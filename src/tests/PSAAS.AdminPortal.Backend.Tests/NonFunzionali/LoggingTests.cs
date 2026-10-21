using System.Net;
using NLog;
using NLog.Config;
using NLog.Targets;
using PSAAS.AdminPortal.Backend.Tests.Infrastruttura;
using Xunit;

namespace PSAAS.AdminPortal.Backend.Tests.NonFunzionali;

/// <summary>
/// Non-functional requirement (no use case): every layer logs through NLog.
/// Same collection as the scenarios, so it never runs in parallel with another host touching NLog.
/// </summary>
public sealed class LoggingTests(PostgresFixture database) : AcceptanceTest(database)
{
    [Fact]
    public async Task ILogDiOgniStratoArrivanoANLog()
    {
        var memory = StartCapturingLogs();

        await GivenTenantAsync("Alfa Srl");

        Assert.Contains(memory.Logs, log =>
            log.StartsWith("PSAAS.Application.Services.Tenants.TenantCreateService|Info|Created tenant", StringComparison.Ordinal));
        Assert.Contains(memory.Logs, log =>
            log.StartsWith("PSAAS.Infrastructure.Repositories.Tenants.TenantWriteRepository|Debug|", StringComparison.Ordinal));
        Assert.Contains(memory.Logs, log =>
            log.StartsWith("PSAAS.AdminPortal.Backend.Controllers.TenantsController|Info|Returned tenant", StringComparison.Ordinal));
        Assert.Contains(memory.Logs, log =>
            log.StartsWith("Microsoft.AspNetCore.Hosting.Diagnostics|Info|Request finished HTTP/1.1 POST", StringComparison.Ordinal));
    }

    [Fact]
    public async Task InDevelopmentLogPsaasADebugEFrameworkSoloDaWarning()
    {
        await Admin.GetAsync("/health");

        Assert.True(LogManager.GetLogger("PSAAS.Infrastructure.Repositories.Tenants.TenantReadRepository").IsDebugEnabled);
        Assert.True(LogManager.GetLogger("Microsoft.AspNetCore.Hosting.Diagnostics").IsInfoEnabled);
        Assert.False(LogManager.GetLogger("Microsoft.AspNetCore.Routing.EndpointMiddleware").IsInfoEnabled);
        Assert.True(LogManager.GetLogger("Microsoft.AspNetCore.Routing.EndpointMiddleware").IsWarnEnabled);
    }

    private MemoryTarget StartCapturingLogs()
    {
        // The host configures NLog when it starts: start it first, then add the capture rule.
        Assert.Equal(HttpStatusCode.OK, Admin.GetAsync("/health").Result.StatusCode);
        var memory = new MemoryTarget("memory") { Layout = "${logger}|${level}|${message}" };
        // First rule: the request-log rule is final and would stop messages before a rule added at the end.
        LogManager.Configuration!.LoggingRules.Insert(0, new LoggingRule("*", NLog.LogLevel.Trace, memory));
        LogManager.ReconfigExistingLoggers();
        return memory;
    }
}
