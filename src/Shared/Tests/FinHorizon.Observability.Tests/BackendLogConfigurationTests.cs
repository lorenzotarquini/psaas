using System.Text.Json;
using Xunit;

namespace FinHorizon.Observability.Tests;

/// <summary>
/// The NLog configuration of every backend, as Alloy and the Grafana dashboards expect it: JSON lines named after the
/// service, with the request scope (the pseudonymous user), and nothing that identifies a person.
/// </summary>
public sealed class BackendLogConfigurationTests
{
    public static TheoryData<string, string> Backends => new()
    {
        { "MySync/FinHorizon.MySync.Backend", "mysync-backend" },
        { "AdminPortal/FinHorizon.AdminPortal.Backend", "adminportal-backend" },
    };

    private static string SourceRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "FinHorizon.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("FinHorizon.slnx not found above the test output.");
    }

    private static JsonElement NLogOf(string backend, string file = "appsettings.json") =>
        JsonDocument.Parse(File.ReadAllText(Path.Combine(SourceRoot(), backend, file))).RootElement.GetProperty("NLog");

    [Theory]
    [MemberData(nameof(Backends))]
    public void JsonTargets_NameTheServiceAndCarryTheRequestScope(string backend, string service)
    {
        var targets = NLogOf(backend).GetProperty("targets");

        foreach (var name in new[] { "stdout", "jsonFile" })
        {
            var layout = targets.GetProperty(name).GetProperty("layout");
            Assert.True(layout.GetProperty("includeScopeProperties").GetBoolean());
            Assert.True(layout.GetProperty("includeEventProperties").GetBoolean());
            // ASP.NET Core puts the actual path in the request scope: it can hold ids and emails.
            Assert.Contains("RequestPath", layout.GetProperty("excludeProperties").GetString()!.Split(','));
            Assert.Contains(layout.GetProperty("attributes").EnumerateArray(), attribute =>
                attribute.GetProperty("name").GetString() == "service" && attribute.GetProperty("layout").GetString() == service);
        }

        Assert.EndsWith($"/{service}.log", targets.GetProperty("jsonFile").GetProperty("fileName").GetString(), StringComparison.Ordinal);
    }

    [Theory]
    [MemberData(nameof(Backends))]
    public void NoTarget_WritesTheUserNameOrTheUrl(string backend, string service)
    {
        var text = NLogOf(backend).GetRawText();

        Assert.DoesNotContain("aspnet-user", text, StringComparison.Ordinal);
        Assert.DoesNotContain("aspnet-request-url", text, StringComparison.Ordinal);
        Assert.NotEmpty(service);
    }

    [Theory]
    [MemberData(nameof(Backends))]
    public void Development_WritesReadableConsoleAndTheJsonFileAlloyReads(string backend, string service)
    {
        var rules = NLogOf(backend, "appsettings.Development.json").GetProperty("rules");

        foreach (var rule in new[] { "0", "1", "4" })
        {
            Assert.Equal("console,jsonFile", rules.GetProperty(rule).GetProperty("writeTo").GetString());
        }

        var console = NLogOf(backend).GetProperty("targets").GetProperty("console").GetProperty("layout").GetString();
        Assert.StartsWith("${date:", console, StringComparison.Ordinal);
        Assert.Contains("|${level:uppercase=true}|${logger}|${message}", console, StringComparison.Ordinal);
        Assert.NotEmpty(service);
    }
}
