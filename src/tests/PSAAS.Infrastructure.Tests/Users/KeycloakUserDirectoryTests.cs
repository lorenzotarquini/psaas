using System.Net;
using System.Text.Json;
using PSAAS.Application.Abstractions.Users;
using PSAAS.Infrastructure.Users;
using Xunit;

namespace PSAAS.Infrastructure.Tests.Users;

/// <summary>Derived from UC6 and UC7: the HTTP calls to the Keycloak admin API that the acceptance tests simulate.</summary>
public sealed class KeycloakUserDirectoryTests
{
    private const string BaseUrl = "https://kc.test/admin/realms/PSAAS/";

    private sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public List<(HttpMethod Method, string Url, string? Body)> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Requests.Add((request.Method, request.RequestUri!.ToString(),
                request.Content is null ? null : await request.Content.ReadAsStringAsync(ct)));
            return respond(request);
        }
    }

    private static KeycloakUserDirectory Directory(Handler handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri(BaseUrl) });

    private static readonly NewDirectoryUser Mario =
        new("Mario", "Rossi", "mario.rossi@example.com", new DateOnly(1980, 5, 17), ["p1", "p2"]);

    private static HttpResponseMessage Created(string id) =>
        new(HttpStatusCode.Created) { Headers = { Location = new Uri($"{BaseUrl}users/{id}") } };

    [Fact]
    public async Task UC6_S1_Create_StoresAttributesAndAsksKeycloakToEmailThePasswordLink()
    {
        var handler = new Handler(request => request.Method == HttpMethod.Post ? Created("kc-1") : new(HttpStatusCode.NoContent));

        var user = await Directory(handler).CreateAsync(Mario);

        Assert.Equal("kc-1", user.Id);
        var (method, url, body) = handler.Requests[0];
        Assert.Equal((HttpMethod.Post, $"{BaseUrl}users"), (method, url));
        var json = JsonDocument.Parse(body!).RootElement;
        Assert.Equal("mario.rossi@example.com", json.GetProperty("username").GetString());
        Assert.Equal("1980-05-17", json.GetProperty("attributes").GetProperty("birthDate")[0].GetString());
        Assert.Equal(2, json.GetProperty("attributes").GetProperty("profileIds").GetArrayLength());
        Assert.Equal(
            (HttpMethod.Put, $"{BaseUrl}users/kc-1/execute-actions-email", """["UPDATE_PASSWORD"]"""),
            handler.Requests[1]);
    }

    [Fact]
    public async Task UC6_S5_Create_KeycloakConflict_IsAnEmailConflict()
    {
        var handler = new Handler(_ => new(HttpStatusCode.Conflict));

        await Assert.ThrowsAsync<UserEmailConflictException>(() => Directory(handler).CreateAsync(Mario));
    }

    [Fact]
    public async Task UC6_S7_Create_KeycloakDown_IsUnavailable()
    {
        var handler = new Handler(_ => throw new HttpRequestException("connection refused"));

        await Assert.ThrowsAsync<UserDirectoryUnavailableException>(() => Directory(handler).CreateAsync(Mario));
    }

    [Fact]
    public async Task UC6_3d_Create_EmailNotSent_RemovesTheUser()
    {
        var handler = new Handler(request => request.Method == HttpMethod.Post ? Created("kc-1")
            : request.Method == HttpMethod.Put ? new(HttpStatusCode.InternalServerError)
            : new(HttpStatusCode.NoContent));

        await Assert.ThrowsAsync<UserDirectoryUnavailableException>(() => Directory(handler).CreateAsync(Mario));

        Assert.Equal((HttpMethod.Delete, $"{BaseUrl}users/kc-1"), (handler.Requests[2].Method, handler.Requests[2].Url));
    }

    [Fact]
    public async Task UC7_S1_Search_IsInfixAndReadsTheAttributes()
    {
        const string Json = """
            [{"id":"kc-1","username":"mario.rossi@example.com","email":"mario.rossi@example.com","firstName":"Mario","lastName":"Rossi",
              "attributes":{"birthDate":["1980-05-17"],"profileIds":["p1","p2"]}},
             {"id":"kc-2","username":"admin","firstName":"Ada"}]
            """;
        var handler = new Handler(_ => new(HttpStatusCode.OK) { Content = new StringContent(Json) });

        var users = await Directory(handler).SearchAsync(" mario ");

        Assert.Equal($"{BaseUrl}users?briefRepresentation=false&max=100&search=%2Amario%2A", handler.Requests[0].Url);
        Assert.Equal(new DateOnly(1980, 5, 17), users[0].BirthDate);
        Assert.Equal(["p1", "p2"], users[0].ProfileIds);
        // Users created outside the portal: no attributes, no email.
        Assert.Equal(("admin", null), (users[1].Email, users[1].BirthDate));
        Assert.Empty(users[1].ProfileIds);
    }

    [Fact]
    public async Task UC7_Search_Empty_ListsEveryone()
    {
        var handler = new Handler(_ => new(HttpStatusCode.OK) { Content = new StringContent("[]") });

        Assert.Empty(await Directory(handler).SearchAsync("  "));
        Assert.Equal($"{BaseUrl}users?briefRepresentation=false&max=100", handler.Requests[0].Url);
    }

    [Fact]
    public async Task UC7_S6_Search_KeycloakError_IsUnavailable()
    {
        var handler = new Handler(_ => new(HttpStatusCode.BadGateway));

        await Assert.ThrowsAsync<UserDirectoryUnavailableException>(() => Directory(handler).SearchAsync("mario"));
    }

    [Fact]
    public async Task Search_ServiceAccountWithoutRights_IsAConfigurationError()
    {
        var handler = new Handler(_ => new(HttpStatusCode.Forbidden));

        await Assert.ThrowsAsync<InvalidOperationException>(() => Directory(handler).SearchAsync("mario"));
    }
}
