using System.Net;
using System.Net.Http.Json;
using PSAAS.AdminPortal.Backend.Tests.Infrastruttura;
using Xunit;

namespace PSAAS.AdminPortal.Backend.Tests;

/// <summary>docs/use-cases/UC12-audit-log.md</summary>
public sealed class UC12_AuditLog(PostgresFixture database) : AcceptanceTest(database)
{
    [Fact, Trait("Scenario", "UC12-S1")]
    public async Task S1_AttivazioneRegistrataConAutore()
    {
        var alfa = await GivenTenantAsync("Alfa Srl");

        var e = Assert.Single((await AuditAsync()).Items);
        Assert.Equal(("tenant.created", "Alfa Srl", alfa.Id.ToString()), (e.Action, e.TargetName, e.TenantId));
        Assert.Equal((KeycloakTokens.AdminSubject, KeycloakTokens.AdminUserName), (e.ActorId, e.ActorName));
    }

    [Fact, Trait("Scenario", "UC12-S2")]
    public async Task S2_ModificaAnagraficaConPrimaEDopo()
    {
        var alfa = await GivenTenantAsync("Alfa Srl", certifiedEmail: "alfa@pec.it");

        await Admin.PutAsJsonAsync($"/api/tenants/{alfa.Id}", TenantBody(
            alfa.CompanyName, alfa.CompleteVatNumber, "nuova@pec.it", alfa.Address.Street, alfa.Address.City, alfa.Address.PostalCode));

        var e = (await AuditAsync("?action=tenant.registry")).Items.Single();
        var changes = e.Details!.Value.GetProperty("changes");
        Assert.Equal(["certifiedEmail"], changes.EnumerateObject().Select(change => change.Name));
        Assert.Equal("alfa@pec.it", changes.GetProperty("certifiedEmail").GetProperty("before").GetString());
        Assert.Equal("nuova@pec.it", changes.GetProperty("certifiedEmail").GetProperty("after").GetString());
    }

    [Fact, Trait("Scenario", "UC12-S3")]
    public async Task S3_ModificaOrganizzazioneConAggiunteETolte()
    {
        var alfa = await GivenTenantAsync("Alfa Srl", profiles: new[] { new { key = "op", name = "Operatore" } });

        await Admin.PutAsJsonAsync($"/api/tenants/{alfa.Id}/organization", new
        {
            profiles = new[] { new { id = (string?)null, key = "rev", name = "Revisore" } },
            productiveUnits = Array.Empty<object>(),
        });

        var details = (await AuditAsync("?action=tenant.organization")).Items.Single().Details!.Value;
        Assert.Equal(["Revisore"], details.GetProperty("profilesAdded").EnumerateArray().Select(p => p.GetString()));
        Assert.Equal(["Operatore"], details.GetProperty("profilesRemoved").EnumerateArray().Select(p => p.GetString()));
    }

    [Fact, Trait("Scenario", "UC12-S4")]
    public async Task S4_UtenteNelloStoricoDiOgniCliente()
    {
        var alfa = await GivenTenantAsync("Alfa Srl", profiles: new[] { new { key = "op", name = "Operatore" } });
        var beta = await GivenTenantAsync("Beta Snc", profiles: new[] { new { key = "rev", name = "Revisore" } });

        await Admin.PostAsJsonAsync("/api/users", new
        {
            firstName = "Mario",
            lastName = "Rossi",
            email = "mario.rossi@example.com",
            birthDate = "1980-05-17",
            profileIds = new[] { alfa.Profile("Operatore").Id, beta.Profile("Revisore").Id },
        });

        foreach (var tenant in new[] { alfa, beta })
        {
            var history = await AuditAsync($"?tenantId={tenant.Id}&action=user.");
            Assert.Equal("Mario Rossi", Assert.Single(history.Items).TargetName);
        }
    }

    [Fact, Trait("Scenario", "UC12-S5")]
    public async Task S5_StoricoDiUnSoloCliente()
    {
        var alfa = await GivenTenantAsync("Alfa Srl");
        await GivenTenantAsync("Beta Snc");
        await Admin.PutAsJsonAsync($"/api/tenants/{alfa.Id}/organization", new
        {
            profiles = new[] { new { id = (string?)null, key = "op", name = "Operatore" } },
            productiveUnits = Array.Empty<object>(),
        });

        var history = await AuditAsync($"?tenantId={alfa.Id}");

        Assert.Equal(["tenant.organization.updated", "tenant.created"], history.Items.Select(e => e.Action));
    }

    [Fact, Trait("Scenario", "UC12-S6")]
    public async Task S6_FiltriPerAzioneETesto()
    {
        await GivenTenantAsync("Alfa Srl");
        await GivenTenantAsync("Beta Snc");
        await Admin.PostAsJsonAsync("/api/users", new
        {
            firstName = "Mario",
            lastName = "Rossi",
            email = "mario.rossi@example.com",
            birthDate = "1980-05-17",
            profileIds = Array.Empty<string>(),
        });

        Assert.Equal(["Beta Snc", "Alfa Srl"], (await AuditAsync("?action=tenant.")).Items.Select(e => e.TargetName));
        Assert.Equal(["Beta Snc"], (await AuditAsync("?search=beta")).Items.Select(e => e.TargetName));
    }

    [Fact, Trait("Scenario", "UC12-S7")]
    public async Task S7_PagineLimitate()
    {
        var response = await Admin.GetAsync("/api/audit?pageSize=500");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private Task<PageView<AuditEventView>> AuditAsync(string query = "") => GetOkAsync<PageView<AuditEventView>>($"/api/audit{query}");
}
