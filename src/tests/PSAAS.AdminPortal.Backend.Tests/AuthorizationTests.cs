using System.Net;
using System.Net.Http.Headers;
using Xunit;

namespace PSAAS.AdminPortal.Backend.Tests;

public sealed class AuthorizationTests(BackendFactory factory) : IClassFixture<BackendFactory>
{
    [Fact]
    public async Task Api_WithoutToken_Returns401()
    {
        var response = await factory.CreateClient().GetAsync("/api/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Api_WithTokenWithoutAdministratorRole_Returns403()
    {
        var response = await SendAsync(BackendFactory.CreateToken(realmRoles: "User"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Api_WithTokenForAnotherAudience_Returns401()
    {
        var response = await SendAsync(BackendFactory.CreateToken("another-client", "Administrator"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Api_WithAdministratorRole_Returns200WithUser()
    {
        var response = await SendAsync(BackendFactory.CreateToken(realmRoles: "Administrator"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("test.user", body);
        Assert.Contains("Administrator", body);
    }

    [Fact]
    public async Task Health_WithoutToken_Returns200()
    {
        var response = await factory.CreateClient().GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private async Task<HttpResponseMessage> SendAsync(string token)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await client.GetAsync("/api/me");
    }
}
