using System.Net;
using PSAAS.AdminPortal.Backend.Tests.Infrastruttura;
using Xunit;

namespace PSAAS.AdminPortal.Backend.Tests;

/// <summary>docs/use-cases/UC7-cercare-utenti.md</summary>
public sealed class UC7_CercareUtenti(PostgresFixture database) : AcceptanceTest(database)
{
    [Fact, Trait("Scenario", "UC7-S1")]
    public async Task S1_RicercaPerNomeConProfiliEClienti()
    {
        var alfa = await GivenTenantAsync("Alfa Srl", profiles: new[] { new { key = "op", name = "Operatore" } });
        var operatore = alfa.Profile("Operatore");
        KeycloakUsers.Add("Mario", "Rossi", "mario.rossi@example.com", operatore.Id);
        KeycloakUsers.Add("Anna", "Bianchi", "anna.bianchi@example.com");

        var mario = Assert.Single(await SearchAsync("mario"));

        Assert.Equal(("Mario", "Rossi"), (mario.FirstName, mario.LastName));
        Assert.Equal(new UserProfileView(operatore.Id, "Operatore", alfa.Id.ToString(), "Alfa Srl"), Assert.Single(mario.Profiles));
    }

    [Fact, Trait("Scenario", "UC7-S2")]
    public async Task S2_RicercaPerEmailSenzaMaiuscole()
    {
        KeycloakUsers.Add("Anna", "Bianchi", "anna.bianchi@example.com");

        var anna = Assert.Single(await SearchAsync("ANNA.BIANCHI@"));

        Assert.Equal("anna.bianchi@example.com", anna.Email);
    }

    [Fact, Trait("Scenario", "UC7-S3")]
    public async Task S3_RicercaGlobaleSuTuttiIClienti()
    {
        var alfa = await GivenTenantAsync("Alfa Srl", profiles: new[] { new { key = "op", name = "Operatore" } });
        var beta = await GivenTenantAsync("Beta Snc", profiles: new[] { new { key = "rev", name = "Revisore" } });
        KeycloakUsers.Add("Mario", "Rossi", "mario.rossi@example.com", alfa.Profile("Operatore").Id);
        KeycloakUsers.Add("Mario", "Verdi", "mario.verdi@example.com", beta.Profile("Revisore").Id);

        var users = await SearchAsync("mario");

        Assert.Equal(["Rossi", "Verdi"], users.Select(user => user.LastName).Order());
    }

    [Fact, Trait("Scenario", "UC7-S4")]
    public async Task S4_ProfiliCancellatiNonMostrati()
    {
        var alfa = await GivenTenantAsync("Alfa Srl", profiles: new[] { new { key = "op", name = "Operatore" } });
        KeycloakUsers.Add("Mario", "Rossi", "mario.rossi@example.com", alfa.Profile("Operatore").Id, Guid.NewGuid().ToString());

        var mario = Assert.Single(await SearchAsync("mario"));

        Assert.Equal(["Operatore"], mario.Profiles.Select(profile => profile.Name));
    }

    [Fact, Trait("Scenario", "UC7-S5")]
    public async Task S5_NessunaCorrispondenza()
    {
        KeycloakUsers.Add("Anna", "Bianchi", "anna.bianchi@example.com");

        Assert.Empty(await SearchAsync("zeta"));
    }

    [Fact, Trait("Scenario", "UC7-S6")]
    public async Task S6_KeycloakNonRaggiungibile()
    {
        KeycloakUsers.Unavailable = true;

        var response = await Admin.GetAsync("/api/users?search=mario");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    private Task<List<UserView>> SearchAsync(string search) =>
        GetOkAsync<List<UserView>>($"/api/users?search={Uri.EscapeDataString(search)}");
}
