using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NLog;
using NLog.Layouts;
using NLog.Targets;
using Xunit;

namespace FinHorizon.Observability.Tests;

/// <summary>
/// What every backend line must carry for Loki and Grafana: the pseudonymous user and one structured line per request,
/// never a name, an email or the actual URL.
/// </summary>
[Collection(NLogCollection.Name)]
public sealed class RequestLoggingMiddlewareTests : IAsyncLifetime
{
    private const string Subject = "6f1c2a7e-3b9d-4c55-9e1a-2f8d0b7c4e11";
    private const string Username = "mario.rossi";
    private const string Email = "mario.rossi@example.com";

    private readonly MemoryTarget _memory = new("memory")
    {
        Layout = new JsonLayout
        {
            IncludeEventProperties = true,
            IncludeScopeProperties = true,
            Attributes =
            {
                new JsonAttribute("level", "${level:uppercase=true}"),
                new JsonAttribute("logger", "${logger}"),
                new JsonAttribute("message", "${message}"),
            },
        },
    };

    private IHost _host = null!;

    public async Task InitializeAsync()
    {
        var configuration = new NLog.Config.LoggingConfiguration();
        configuration.AddRuleForAllLevels(_memory);
        LogManager.Configuration = configuration;

        _host = await new HostBuilder()
            .ConfigureWebHost(web => web
                .UseTestServer()
                .ConfigureServices(services => services.AddRouting())
                .Configure(app =>
                {
                    // Stands in for UseAuthentication: the token of a signed-in user, with name and email as Keycloak puts them.
                    app.Use(async (context, next) =>
                    {
                        if (context.Request.Headers.ContainsKey("X-Signed-In"))
                        {
                            context.User = new ClaimsPrincipal(new ClaimsIdentity(
                                [new Claim("sub", Subject), new Claim("preferred_username", Username), new Claim("email", Email)],
                                "Test",
                                "preferred_username",
                                "role"));
                        }

                        await next(context);
                    });
                    app.UseRouting();
                    app.UseRequestLogging();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGet("/api/items/{id:int}", (int id) =>
                        {
                            LogManager.GetLogger("Items").Info("Item {ItemId} read", id);
                            return Results.Ok(id);
                        });
                        endpoints.MapGet("/api/broken", (Func<IResult>)(() => throw new InvalidOperationException("boom")));
                        endpoints.MapGet("/health", () => Results.Ok());
                    });
                }))
            .StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _host.StopAsync();
        _host.Dispose();
        LogManager.Configuration = null;
    }

    private async Task<HttpResponseMessage> GetAsync(string url, bool signedIn = true)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        if (signedIn)
        {
            request.Headers.Add("X-Signed-In", "1");
        }

        return await _host.GetTestClient().SendAsync(request);
    }

    private List<JsonElement> Lines() => _memory.Logs.Select(line => JsonDocument.Parse(line).RootElement).ToList();

    private JsonElement RequestLine() => Assert.Single(Lines(), line => line.TryGetProperty(LogFields.Event, out var kind) && kind.GetString() == LogEvents.HttpRequest);

    [Fact]
    public async Task OneLinePerRequest_WithRouteTemplateStatusAndDuration()
    {
        await GetAsync("/api/items/42?term=secret");

        var line = RequestLine();
        Assert.Equal("GET", line.GetProperty(LogFields.Method).GetString());
        Assert.Equal("/api/items/{id:int}", line.GetProperty(LogFields.Route).GetString());
        Assert.Equal(200, line.GetProperty(LogFields.StatusCode).GetInt32());
        Assert.True(line.GetProperty(LogFields.ElapsedMs).GetInt64() >= 0);
        Assert.Equal("INFO", line.GetProperty("level").GetString());
        Assert.DoesNotContain(_memory.Logs, text => text.Contains("secret", StringComparison.Ordinal) || text.Contains("/42", StringComparison.Ordinal));
    }

    [Fact]
    public async Task EveryLineOfTheRequest_HasThePseudonymousUserAndNoPersonalData()
    {
        await GetAsync("/api/items/42");

        var lines = Lines();
        Assert.Equal(2, lines.Count);
        Assert.All(lines, line => Assert.Equal(Subject, line.GetProperty(LogFields.UserId).GetString()));
        Assert.DoesNotContain(_memory.Logs, text => text.Contains(Username, StringComparison.Ordinal) || text.Contains(Email, StringComparison.Ordinal));
    }

    [Fact]
    public async Task AnonymousRequest_HasNoUser()
    {
        await GetAsync("/api/items/42", signedIn: false);

        Assert.False(RequestLine().TryGetProperty(LogFields.UserId, out _));
    }

    [Fact]
    public async Task UnknownPath_IsLoggedAsUnmatchedAndWarn()
    {
        var response = await GetAsync("/api/customers/mario.rossi@example.com");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var line = RequestLine();
        Assert.Equal("unmatched", line.GetProperty(LogFields.Route).GetString());
        Assert.Equal("WARN", line.GetProperty("level").GetString());
        Assert.DoesNotContain(_memory.Logs, text => text.Contains(Email, StringComparison.Ordinal));
    }

    [Fact]
    public async Task Exception_IsLoggedAs500AtError()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => GetAsync("/api/broken"));

        var line = RequestLine();
        Assert.Equal(500, line.GetProperty(LogFields.StatusCode).GetInt32());
        Assert.Equal("ERROR", line.GetProperty("level").GetString());
    }

    [Fact]
    public async Task HealthProbes_OnlyAtDebug()
    {
        await GetAsync("/health", signedIn: false);

        Assert.Equal("DEBUG", RequestLine().GetProperty("level").GetString());
    }
}

/// <summary>NLog's configuration is global: these tests never run in parallel with each other.</summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class NLogCollection
{
    public const string Name = "NLog";
}
