using PSAAS.AdminPortal.Backend.Tests.Infrastruttura;
using Xunit;

namespace PSAAS.AdminPortal.Backend.Tests;

/// <summary>docs/use-cases/UC9-panoramica-profili.md</summary>
public sealed class UC9_PanoramicaProfili(PostgresFixture database) : AcceptanceTest(database)
{
    [Fact, Trait("Scenario", "UC9-S1")]
    public async Task S1_TuttiIProfiliConIlLoroCliente()
    {
        var alfa = await GivenTenantAsync("Alfa Srl", profiles: new[] { new { key = "op", name = "Operatore" } });
        var beta = await GivenTenantAsync("Beta Snc", profiles: new[] { new { key = "amm", name = "Amministrativo" } });

        var profiles = await GetOkAsync<List<ProfileOverview>>("/api/profiles");

        Assert.Equal(
            [("Amministrativo", beta.Id.ToString(), "Beta Snc"), ("Operatore", alfa.Id.ToString(), "Alfa Srl")],
            profiles.Select(profile => (profile.Name, profile.TenantId, profile.TenantCompanyName)));
    }

    [Fact, Trait("Scenario", "UC9-S2")]
    public async Task S2_NessunProfilo()
    {
        Assert.Empty(await GetOkAsync<List<ProfileOverview>>("/api/profiles"));
    }
}
