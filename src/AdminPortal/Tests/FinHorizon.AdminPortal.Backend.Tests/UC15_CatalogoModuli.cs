using System.Net;
using System.Net.Http.Json;
using FinHorizon.AdminPortal.Backend.Tests.Infrastruttura;
using Xunit;

namespace FinHorizon.AdminPortal.Backend.Tests;

/// <summary>docs/use-cases/UC15-catalogo-moduli.md</summary>
public sealed class UC15_CatalogoModuli(PostgresFixture database) : AcceptanceTest(database)
{
    [Fact, Trait("Scenario", "UC15-S1")]
    public async Task S1_MySyncNelCatalogoSenzaCore()
    {
        var modules = await GetOkAsync<List<ModuleView>>("/api/modules");

        Assert.Equal("Available", modules.Single(m => m.Id == "mysync").Status);
        Assert.DoesNotContain(modules, m => m.Id == "core");
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
    public async Task S5_DeprecareUnModulo()
    {
        await GivenModuleAsync("warehouse");

        var deprecated = await ReadAsync<ModuleView>(await Admin.PutAsJsonAsync("/api/modules/warehouse/status", new { status = "Deprecated" }), HttpStatusCode.OK);
        var errors = await ErrorsAsync(await Admin.PutAsJsonAsync("/api/modules/warehouse/status", new { status = "Retired" }));

        Assert.Equal("Deprecated", deprecated.Status);
        Assert.Equal("InvalidValue", errors["status"]);
    }

    [Fact, Trait("Scenario", "UC15-S6")]
    public async Task S6_TierDalManifest()
    {
        var tiers = new object[]
        {
            new { level = 1, name = "Base", features = Array.Empty<string>(), limits = new Dictionary<string, int> { ["items"] = 100 } },
            new { level = 2, name = "Pro", features = new[] { "reports" }, limits = new Dictionary<string, int> { ["items"] = 1000 } },
        };

        var saved = await ReadAsync<ModuleView>(
            await Admin.PostAsJsonAsync("/api/modules", new { id = "warehouse", name = "Magazzino", version = "1.0.0", tiers }), HttpStatusCode.OK);
        var mysync = (await GetOkAsync<List<ModuleView>>("/api/modules")).Single(m => m.Id == "mysync");

        Assert.Equal(["Base", "Pro"], saved.Tiers!.Select(t => t.Name));
        Assert.Equal(["reports"], saved.Tiers![1].Features!);
        Assert.Equal(1000, saved.Tiers[1].Limits!["items"]);
        // The manifest of MySync, seeded by the migration (docs/architettura-moduli.md).
        Assert.Equal(["Base", "Plus", "Pro"], mysync.Tiers!.Select(t => t.Name));
    }

    [Fact, Trait("Scenario", "UC15-S7")]
    public async Task S7_TierNonValidi()
    {
        var tiers = new object[]
        {
            new { level = 1, name = "Pro", features = new[] { "reports" } },
            new { level = 2, name = "Base", features = Array.Empty<string>() },
        };

        var errors = await ErrorsAsync(await Admin.PostAsJsonAsync("/api/modules", new { id = "warehouse", name = "Magazzino", version = "1.0.0", tiers }));

        Assert.Equal("InvalidValue", errors["tiers[1]"]);
    }

    [Fact, Trait("Scenario", "UC15-S8")]
    public async Task S8_ModificareUnModulo()
    {
        var tiers = new object[]
        {
            new { level = 1, name = "Essenziale", features = Array.Empty<string>(), limits = new Dictionary<string, int> { ["items"] = 3 } },
            new { level = 2, name = "Pro", features = new[] { "reports" }, limits = new Dictionary<string, int> { ["items"] = 10 } },
        };
        await Admin.PostAsJsonAsync("/api/modules", new { id = "warehouse", name = "Magazzino", version = "1.0.0" });

        var saved = await ReadAsync<ModuleView>(
            await Admin.PutAsJsonAsync("/api/modules/warehouse", new { name = "Magazzino", version = "2.0.0", tiers }), HttpStatusCode.OK);
        var renamed = await ErrorsAsync(await Admin.PutAsJsonAsync("/api/modules/warehouse", new { id = "altro", name = "Magazzino", version = "2.0.0" }));
        var missing = await Admin.PutAsJsonAsync("/api/modules/nope", new { name = "Nope", version = "1.0.0" });

        Assert.Equal("warehouse", saved.Id);
        Assert.Equal("2.0.0", saved.Version);
        Assert.Equal(["Essenziale", "Pro"], saved.Tiers!.Select(t => t.Name));
        Assert.Equal(3, saved.Tiers[0].Limits!["items"]);
        Assert.Equal("InvalidValue", renamed["id"]);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }
}
