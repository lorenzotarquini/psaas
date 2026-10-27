using System.Net;
using Microsoft.Extensions.Options;
using Xunit;

namespace PSAAS.Auth.Tests;

public sealed class KeycloakClientCredentialsHandlerTests
{
    [Fact]
    public async Task RejectedCredentials_FailAsMisconfiguredWithoutRetrying()
    {
        var tokenEndpoint = new CountingHandler(HttpStatusCode.Unauthorized);
        using var client = CreateClient(tokenEndpoint);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => client.GetAsync("https://keycloak.example/admin/realms/psaas/users"));

        Assert.Contains("HTTP 401", exception.Message);
        Assert.Equal(1, tokenEndpoint.Calls);
    }

    [Fact]
    public async Task ServerErrors_AreRetriedAndStayHttpErrors()
    {
        var tokenEndpoint = new CountingHandler(HttpStatusCode.BadGateway);
        using var client = CreateClient(tokenEndpoint);

        await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync("https://keycloak.example/admin/realms/psaas/users"));

        Assert.Equal(3, tokenEndpoint.Calls);
    }

    private static HttpClient CreateClient(HttpMessageHandler tokenEndpoint)
    {
        var options = Options.Create(new KeycloakOptions
        {
            Authority = "https://keycloak.example/realms/psaas",
            ClientId = "adminportal-api",
            ClientSecret = "wrong",
        });
        var handler = new KeycloakClientCredentialsHandler(options, new Factory(tokenEndpoint))
        {
            InnerHandler = new CountingHandler(HttpStatusCode.OK),
        };
        return new HttpClient(handler);
    }

    private sealed class Factory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    private sealed class CountingHandler(HttpStatusCode status) : HttpMessageHandler
    {
        public int Calls { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(new HttpResponseMessage(status));
        }
    }
}
