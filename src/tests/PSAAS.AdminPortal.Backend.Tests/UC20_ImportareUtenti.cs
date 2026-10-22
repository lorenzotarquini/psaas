using System.Net;
using System.Net.Http.Json;
using PSAAS.AdminPortal.Backend.Tests.Infrastruttura;
using Xunit;

namespace PSAAS.AdminPortal.Backend.Tests;

/// <summary>docs/use-cases/UC20-importare-utenti.md</summary>
public sealed class UC20_ImportareUtenti(PostgresFixture database) : AcceptanceTest(database)
{
    private static object Row(string email, string profiles = "Alfa Srl:Operatore") =>
        new { firstName = "Mario", lastName = "Rossi", email, birthDate = "1980-01-01", profiles };

    private async Task<List<ImportRowView>> ImportAsync(bool confirm, params object[] rows) =>
        await ReadAsync<List<ImportRowView>>(await Admin.PostAsJsonAsync("/api/users/import", new { rows, confirm }), HttpStatusCode.OK);

    [Fact, Trait("Scenario", "UC20-S1")]
    public async Task S1_AnteprimaConErrori()
    {
        await GivenTenantAsync("Alfa Srl", profiles: new[] { new { key = "op", name = "Operatore" } });

        var rows = await ImportAsync(false, Row("mario@example.com", "alfa srl:operatore"), Row("non-email"), Row("anna@example.com", "Alfa Srl:Nessuno"));

        Assert.Empty(rows[0].Errors);
        Assert.Equal("InvalidFormat", rows[1].Errors["email"]);
        Assert.Equal("InvalidValue", rows[2].Errors["profiles"]);
        Assert.Empty(KeycloakUsers.Users);
    }

    [Fact, Trait("Scenario", "UC20-S2")]
    public async Task S2_ConfermaCreaLeRigheValide()
    {
        var alfa = await GivenTenantAsync("Alfa Srl", profiles: new[] { new { key = "op", name = "Operatore" } });

        var rows = await ImportAsync(true, Row("mario@example.com"), Row("anna@example.com"), Row("non-email"));

        Assert.Equal(2, rows.Count(r => r.Created is not null));
        Assert.Equal("InvalidFormat", rows[2].Errors["email"]);
        Assert.All(KeycloakUsers.Users, u => Assert.Equal([alfa.Profile("Operatore").Id], u.ProfileIds));
    }

    [Fact, Trait("Scenario", "UC20-S3")]
    public async Task S3_EmailRipetutaNelFile()
    {
        var rows = await ImportAsync(false, Row("mario@example.com", ""), Row("MARIO@example.com", ""));

        Assert.Equal("NotUnique", rows[1].Errors["email"]);
    }
}
