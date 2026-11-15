using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FinHorizon.MySync.Backend.Tests.Infrastruttura;
using Xunit;

namespace FinHorizon.MySync.Backend.Tests;

/// <summary>Access rules common to every MySync endpoint.</summary>
public sealed class GEN_Sicurezza : IClassFixture<MySyncHost>
{
    private readonly MySyncHost _host;

    public GEN_Sicurezza(MySyncHost host) => _host = host;

    [Fact]
    public async Task S1_SenzaAutenticazioneLAccessoENegato()
    {
        var response = await _host.CreateClient().GetAsync("/api/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task S2_UnTokenPerUnAltroClientNonVale()
    {
        var response = await ClientWith(MySyncHost.Token(audience: "adminportal-api")).GetAsync("/api/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task S3_UnUtenteAutenticatoSenzaRuoliEntra()
    {
        var me = await ClientWith(MySyncHost.Token()).GetFromJsonAsync<Me>("/api/me");

        Assert.Equal("kc-mario", me!.KeycloakId);
        Assert.Equal("mario.rossi@example.com", me.Email);
    }

    [Fact]
    public async Task S4_LoStatoDelServizioEPubblico()
    {
        var response = await _host.CreateClient().GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private HttpClient ClientWith(string token)
    {
        var client = _host.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private sealed record Me(string? KeycloakId, string? Name, string? Email, List<string> Roles);
}
