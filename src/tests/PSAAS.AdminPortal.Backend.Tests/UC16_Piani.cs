using System.Net.Http.Json;
using PSAAS.AdminPortal.Backend.Tests.Infrastruttura;
using Xunit;

namespace PSAAS.AdminPortal.Backend.Tests;

/// <summary>docs/use-cases/UC16-piani.md</summary>
public sealed class UC16_Piani(PostgresFixture database) : AcceptanceTest(database)
{
    [Fact, Trait("Scenario", "UC16-S1")]
    public async Task S1_CreareUnPiano()
    {
        await GivenModuleAsync("warehouse");

        var pro = await GivenPlanAsync("pro", 99, maxUsers: 50, moduleIds: "warehouse");

        Assert.Equal(["core", "warehouse"], pro.ModuleIds.Order());
        Assert.Equal((99m, 50), (pro.Price, pro.MaxUsers));
    }

    [Fact, Trait("Scenario", "UC16-S2")]
    public async Task S2_CodiceDuplicato()
    {
        await GivenPlanAsync("pro");

        var errors = await ErrorsAsync(await Admin.PostAsJsonAsync("/api/plans", new { code = "PRO", name = "Pro 2", price = 10, billingPeriod = "Monthly" }));

        Assert.Equal("NotUnique", errors["code"]);
    }

    [Fact, Trait("Scenario", "UC16-S3")]
    public async Task S3_DatiNonValidi()
    {
        var errors = await ErrorsAsync(await Admin.PostAsJsonAsync("/api/plans", new { code = "x1", name = "X", price = -1, billingPeriod = "Weekly", moduleIds = new[] { "nope" } }));

        Assert.Equal("InvalidValue", errors["price"]);
        Assert.Equal("InvalidValue", errors["billingPeriod"]);
        Assert.Equal("InvalidValue", errors["moduleIds[0]"]);
    }
}
