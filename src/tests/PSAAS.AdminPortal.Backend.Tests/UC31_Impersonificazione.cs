using System.Net;
using System.Net.Http.Json;
using PSAAS.AdminPortal.Backend.Tests.Infrastruttura;
using Xunit;

namespace PSAAS.AdminPortal.Backend.Tests;

/// <summary>docs/use-cases/UC31-impersonificazione.md</summary>
public sealed class UC31_Impersonificazione(PostgresFixture database) : AcceptanceTest(database)
{
    private Task<HttpResponseMessage> ImpersonateAsync(string userId, string? reason = "Verifica segnalazione 4521", int? minutes = 15) =>
        Admin.PostAsJsonAsync($"/api/users/{userId}/impersonate", new { reason, minutes });

    [Fact, Trait("Scenario", "UC31-S1")]
    public async Task S1_Impersonare()
    {
        var alfa = await GivenTenantAsync("Alfa Srl", profiles: new[] { new { key = "op", name = "Operatore" } });
        var mario = KeycloakUsers.Add("Mario", "Rossi", "mario@example.com", alfa.Profile("Operatore").Id);

        var ticket = await ReadAsync<TicketView>(await ImpersonateAsync(mario.Id), HttpStatusCode.OK);

        Assert.Equal($"https://platform.test/impersonate#access_token=token-of-{mario.Id}&expires_in=900", ticket.Url);
        Assert.InRange((ticket.ExpiresAt - Host.Clock.GetUtcNow()).TotalMinutes, 14.9, 15.1);
        var e = Assert.Single((await GetOkAsync<PageView<AuditEventView>>($"/api/audit?tenantId={alfa.Id}&action=user.impersonated")).Items);
        Assert.Equal("Verifica segnalazione 4521", e.Details!.Value.GetProperty("reason").GetString());
    }

    [Fact, Trait("Scenario", "UC31-S2")]
    public async Task S2_UtenteNonAttivo()
    {
        var giulia = KeycloakUsers.AddInvited("Giulia", "Ferri", "giulia@example.com", Host.Clock.GetUtcNow());

        Assert.Equal(HttpStatusCode.Conflict, (await ImpersonateAsync(giulia.Id)).StatusCode);
    }

    [Fact, Trait("Scenario", "UC31-S3")]
    public async Task S3_MotivoEDurata()
    {
        var mario = KeycloakUsers.Add("Mario", "Rossi", "mario@example.com");

        var errors = await ErrorsAsync(await ImpersonateAsync(mario.Id, reason: " ", minutes: 120));

        Assert.Equal(("Required", "InvalidValue"), (errors["reason"], errors["minutes"]));
    }

    [Fact, Trait("Scenario", "UC31-S4")]
    public async Task S4_NonConfigurata()
    {
        Host.Impersonation.IsConfigured = false;
        var mario = KeycloakUsers.Add("Mario", "Rossi", "mario@example.com");

        var status = await GetOkAsync<Dictionary<string, bool>>("/api/impersonation");
        var response = await ImpersonateAsync(mario.Id);

        Assert.False(status["configured"]);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}
