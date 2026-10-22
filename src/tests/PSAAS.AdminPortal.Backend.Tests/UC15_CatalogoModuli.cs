using System.Net;
using System.Net.Http.Json;
using PSAAS.AdminPortal.Backend.Tests.Infrastruttura;
using Xunit;

namespace PSAAS.AdminPortal.Backend.Tests;

/// <summary>docs/use-cases/UC15-catalogo-moduli.md</summary>
public sealed class UC15_CatalogoModuli(PostgresFixture database) : AcceptanceTest(database)
{
    [Fact, Trait("Scenario", "UC15-S1")]
    public async Task S1_IlCoreCeSempre()
    {
        var core = (await GetOkAsync<List<ModuleView>>("/api/modules")).Single(m => m.Id == "core");

        Assert.True(core.IsCore);
        Assert.Equal("Available", core.Status);
    }

    [Fact, Trait("Scenario", "UC15-S2")]
    public async Task S2_RegistrareDaManifest()
    {
        await Admin.PostAsJsonAsync("/api/modules", new { id = "warehouse", name = "Magazzino", version = "1.4.0", permissions = new[] { "warehouse.stock.edit" } });

        var warehouse = (await GetOkAsync<List<ModuleView>>("/api/modules")).Single(m => m.Id == "warehouse");
        Assert.Equal(("Magazzino", "1.4.0", "Available"), (warehouse.Name, warehouse.Version, warehouse.Status));
        Assert.Equal(["warehouse.stock.edit"], warehouse.Permissions);
    }

    [Fact, Trait("Scenario", "UC15-S3")]
    public async Task S3_ManifestNonValido()
    {
        var errors = await ErrorsAsync(await Admin.PostAsJsonAsync("/api/modules", new { id = "Magazzino!", name = "Magazzino", version = "uno", requires = new[] { "nope" } }));

        Assert.Equal("InvalidFormat", errors["id"]);
        Assert.Equal("InvalidFormat", errors["version"]);
        Assert.Equal("InvalidValue", errors["requires[0]"]);
    }

    [Fact, Trait("Scenario", "UC15-S4")]
    public async Task S4_NuovaVersione()
    {
        await GivenModuleAsync("warehouse", "1.4.0");

        await GivenModuleAsync("warehouse", "1.5.0");

        var warehouse = Assert.Single(await GetOkAsync<List<ModuleView>>("/api/modules"), m => m.Id == "warehouse");
        Assert.Equal("1.5.0", warehouse.Version);
    }

    [Fact, Trait("Scenario", "UC15-S5")]
    public async Task S5_DeprecareMaNonIlCore()
    {
        await GivenModuleAsync("warehouse");

        var deprecated = await ReadAsync<ModuleView>(await Admin.PutAsJsonAsync("/api/modules/warehouse/status", new { status = "Deprecated" }), HttpStatusCode.OK);
        var errors = await ErrorsAsync(await Admin.PutAsJsonAsync("/api/modules/core/status", new { status = "Deprecated" }));

        Assert.Equal("Deprecated", deprecated.Status);
        Assert.Equal("InvalidValue", errors["status"]);
    }
}
