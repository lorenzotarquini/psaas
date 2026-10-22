using System.Net;
using System.Net.Http.Json;
using PSAAS.AdminPortal.Backend.Tests.Infrastruttura;
using Xunit;

namespace PSAAS.AdminPortal.Backend.Tests;

/// <summary>docs/use-cases/UC19-gestire-utente.md</summary>
public sealed class UC19_GestireUtente(PostgresFixture database) : AcceptanceTest(database)
{
    [Fact, Trait("Scenario", "UC19-S1")]
    public async Task S1_DettaglioConStatoEInvito()
    {
        var giulia = await ReadAsync<UserView>(await CreateUserAsync("Giulia", "Ferri", "giulia@example.com"), HttpStatusCode.Created);

        var detail = await GetOkAsync<UserView>($"/api/users/{giulia.Id}");

        Assert.Equal(("Invited", 1, "admin.psaas"), (detail.Status, detail.InvitesSent, detail.CreatedBy));
    }

    [Fact, Trait("Scenario", "UC19-S2")]
    public async Task S2_ModificaDatiEProfili()
    {
        var alfa = await GivenTenantAsync("Alfa Srl", profiles: new[] { new { key = "op", name = "Operatore" } });
        var beta = await GivenTenantAsync("Beta Snc", profiles: new[] { new { key = "rev", name = "Revisore" } });
        var mario = KeycloakUsers.Add("Mario", "Rossi", "mario@example.com", alfa.Profile("Operatore").Id);

        var updated = await ReadAsync<UserView>(await Admin.PutAsJsonAsync($"/api/users/{mario.Id}", new
        {
            firstName = "Mario", lastName = "Russo", birthDate = "1980-01-01", mfaRequired = true,
            profileIds = new[] { alfa.Profile("Operatore").Id, beta.Profile("Revisore").Id },
        }), HttpStatusCode.OK);

        Assert.Equal(("Russo", true), (updated.LastName, updated.MfaRequired));
        Assert.Equal(["Alfa Srl", "Beta Snc"], updated.Profiles.Select(p => p.TenantCompanyName).Order());
        foreach (var tenant in new[] { alfa.Id, beta.Id })
        {
            var history = await GetOkAsync<PageView<AuditEventView>>($"/api/audit?tenantId={tenant}&action=user.updated");
            Assert.Single(history.Items);
        }
    }

    [Fact, Trait("Scenario", "UC19-S3")]
    public async Task S3_DatiNonValidi()
    {
        var mario = KeycloakUsers.Add("Mario", "Rossi", "mario@example.com");

        var errors = await ErrorsAsync(await Admin.PutAsJsonAsync($"/api/users/{mario.Id}", new
        {
            firstName = " ", lastName = "Rossi", birthDate = "1980-01-01", mfaRequired = false, profileIds = new[] { "nope" },
        }));

        Assert.Equal(("Required", "InvalidValue"), (errors["firstName"], errors["profileIds[0]"]));
    }

    [Fact, Trait("Scenario", "UC19-S4")]
    public async Task S4_ReinvioInvito()
    {
        var giulia = await ReadAsync<UserView>(await CreateUserAsync("Giulia", "Ferri", "giulia@example.com"), HttpStatusCode.Created);

        var invited = await ReadAsync<UserView>(await Admin.PostAsync($"/api/users/{giulia.Id}/invite", null), HttpStatusCode.OK);

        Assert.Equal(2, invited.InvitesSent);
        Assert.Contains((giulia.Id, "invite"), KeycloakUsers.ActionEmails);
    }

    [Fact, Trait("Scenario", "UC19-S5")]
    public async Task S5_AzioneNonConsentita()
    {
        var mario = KeycloakUsers.Add("Mario", "Rossi", "mario@example.com");

        var response = await Admin.PostAsync($"/api/users/{mario.Id}/invite", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact, Trait("Scenario", "UC19-S6")]
    public async Task S6_DisattivaERiattiva()
    {
        var mario = KeycloakUsers.Add("Mario", "Rossi", "mario@example.com");

        var disabled = await ReadAsync<UserView>(await Admin.PostAsync($"/api/users/{mario.Id}/disable", null), HttpStatusCode.OK);
        var reset = await Admin.PostAsync($"/api/users/{mario.Id}/reset-password", null);
        var enabled = await ReadAsync<UserView>(await Admin.PostAsync($"/api/users/{mario.Id}/enable", null), HttpStatusCode.OK);

        Assert.Equal(("Disabled", HttpStatusCode.Conflict, "Active"), (disabled.Status, reset.StatusCode, enabled.Status));
    }

    [Fact, Trait("Scenario", "UC19-S7")]
    public async Task S7_SospesoConIlCliente()
    {
        var alfa = await GivenTenantAsync("Alfa Srl", profiles: new[] { new { key = "op", name = "Operatore" } });
        var mario = KeycloakUsers.Add("Mario", "Rossi", "mario@example.com", alfa.Profile("Operatore").Id);

        await Admin.PostAsJsonAsync($"/api/tenants/{alfa.Id}/status", new { status = "Suspended", reason = "Morosità" });

        Assert.Equal("Suspended", (await GetOkAsync<UserView>($"/api/users/{mario.Id}")).Status);
    }

    [Fact, Trait("Scenario", "UC19-S8")]
    public async Task S8_ResetPassword()
    {
        var alfa = await GivenTenantAsync("Alfa Srl", profiles: new[] { new { key = "op", name = "Operatore" } });
        var mario = KeycloakUsers.Add("Mario", "Rossi", "mario@example.com", alfa.Profile("Operatore").Id);

        var response = await Admin.PostAsync($"/api/users/{mario.Id}/reset-password", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains((mario.Id, "reset"), KeycloakUsers.ActionEmails);
        Assert.Single((await GetOkAsync<PageView<AuditEventView>>($"/api/audit?tenantId={alfa.Id}&action=user.password.reset")).Items);
    }

    private Task<HttpResponseMessage> CreateUserAsync(string firstName, string lastName, string email) =>
        Admin.PostAsJsonAsync("/api/users", new { firstName, lastName, email, birthDate = "1991-04-12", profileIds = Array.Empty<string>() });
}
