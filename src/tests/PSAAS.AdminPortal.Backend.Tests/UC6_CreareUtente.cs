using System.Net;
using System.Net.Http.Json;
using PSAAS.AdminPortal.Backend.Tests.Infrastruttura;
using Xunit;

namespace PSAAS.AdminPortal.Backend.Tests;

/// <summary>docs/use-cases/UC6-creare-utente.md</summary>
public sealed class UC6_CreareUtente(PostgresFixture database) : AcceptanceTest(database)
{
    [Fact, Trait("Scenario", "UC6-S1")]
    public async Task S1_UtenteConProfiliDiClientiDiversi()
    {
        var alfa = await GivenTenantAsync("Alfa Srl", profiles: new[] { new { key = "op", name = "Operatore" } });
        var beta = await GivenTenantAsync("Beta Snc", profiles: new[] { new { key = "rev", name = "Revisore" } });

        var created = await ReadAsync<UserView>(await CreateAsync(UserBody(
            "Mario", "Rossi", "mario.rossi@example.com", "1980-05-17",
            alfa.Profile("Operatore").Id, beta.Profile("Revisore").Id)), HttpStatusCode.Created);

        Assert.Equal("mario.rossi@example.com", created.Email);
        Assert.Equal(new DateOnly(1980, 5, 17), created.BirthDate);
        Assert.Equal(
            [("Operatore", "Alfa Srl"), ("Revisore", "Beta Snc")],
            created.Profiles.Select(profile => (profile.Name, profile.TenantCompanyName)).Order());
        var stored = Assert.Single(KeycloakUsers.Users);
        Assert.Equal("mario.rossi@example.com", stored.Email);
        Assert.Equal(["mario.rossi@example.com"], KeycloakUsers.PasswordSetupEmails);
    }

    [Fact, Trait("Scenario", "UC6-S2")]
    public async Task S2_UtenteSenzaProfili()
    {
        var created = await ReadAsync<UserView>(
            await CreateAsync(UserBody("Anna", "Bianchi", "anna.bianchi@example.com", "1990-01-31")),
            HttpStatusCode.Created);

        Assert.Empty(created.Profiles);
        Assert.Empty(Assert.Single(KeycloakUsers.Users).ProfileIds);
    }

    [Fact, Trait("Scenario", "UC6-S3")]
    public async Task S3_DatiObbligatoriMancanti()
    {
        var errors = await ErrorsAsync(await CreateAsync(new { }));

        foreach (var field in new[] { "firstName", "lastName", "email", "birthDate" })
        {
            Assert.Equal("Required", errors[field]);
        }

        Assert.Empty(KeycloakUsers.Users);
    }

    [Fact, Trait("Scenario", "UC6-S4")]
    public async Task S4_EmailNonValida()
    {
        var errors = await ErrorsAsync(await CreateAsync(UserBody("Mario", "Rossi", "non-una-email", "1980-05-17")));

        Assert.Equal("InvalidFormat", errors["email"]);
    }

    [Fact, Trait("Scenario", "UC6-S5")]
    public async Task S5_EmailGiaUsata()
    {
        KeycloakUsers.Add("Mario", "Rossi", "mario.rossi@example.com");

        var errors = await ErrorsAsync(await CreateAsync(UserBody("Mario", "Rossi", "MARIO.ROSSI@example.com", "1980-05-17")));

        Assert.Equal("NotUnique", errors["email"]);
        Assert.Single(KeycloakUsers.Users);
    }

    [Fact, Trait("Scenario", "UC6-S6")]
    public async Task S6_ProfiloInesistente()
    {
        var errors = await ErrorsAsync(await CreateAsync(UserBody(
            "Mario", "Rossi", "mario.rossi@example.com", "1980-05-17", Guid.NewGuid().ToString())));

        Assert.Equal("InvalidValue", errors["profileIds[0]"]);
        Assert.Empty(KeycloakUsers.Users);
    }

    [Fact, Trait("Scenario", "UC6-S7")]
    public async Task S7_KeycloakNonRaggiungibile()
    {
        KeycloakUsers.Unavailable = true;

        var response = await CreateAsync(UserBody("Mario", "Rossi", "mario.rossi@example.com", "1980-05-17"));

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    private static object UserBody(string firstName, string lastName, string email, string birthDate, params string[] profileIds) =>
        new { firstName, lastName, email, birthDate, profileIds };

    private Task<HttpResponseMessage> CreateAsync(object body) => Admin.PostAsJsonAsync("/api/users", body);
}
