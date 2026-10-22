using PSAAS.Application.Contracts.Catalog;
using PSAAS.Application.Services.Catalog;
using Xunit;

namespace PSAAS.Application.Tests.Services;

/// <summary>Derived from UC15 (catalogo moduli) and UC16 (piani).</summary>
public sealed class CatalogServiceTests
{
    private readonly FakeCatalogRepository _catalog = new();
    private readonly FakeAuditLog _audit = new();

    private CatalogService Service => new(_catalog, _audit);

    private static ModuleManifest Manifest(string id, string version = "1.0.0", string[]? requires = null, string? provisionUrl = null) =>
        new(id, id, null, version, null, null, requires, null, provisionUrl, null);

    private static PlanRequest Plan(string code, decimal? price = 10, string? period = "Monthly", int? trialDays = 0, int? maxUsers = null, string[]? modules = null) =>
        new(code, code, null, price, period, trialDays, maxUsers, null, modules);

    [Fact]
    public async Task UC15_S2_ValidManifest_IsSavedAvailableAndAudited()
    {
        var result = await Service.RegisterModuleAsync(Manifest("warehouse", "1.4.0-beta.1"));

        Assert.True(result.Succeeded);
        Assert.Equal(ModuleStatuses.Available, _catalog.Modules.Single(m => m.Id == "warehouse").Status);
        Assert.Equal("module.registered", Assert.Single(_audit.Records).Action);
    }

    [Theory]
    [InlineData("ware house", "1.0.0", null, "id", "InvalidFormat")]
    [InlineData("warehouse", "1.0", null, "version", "InvalidFormat")]
    [InlineData("warehouse", "1.0.0", "ftp://x", "provisionUrl", "InvalidFormat")]
    public async Task UC15_S3_InvalidManifest_IsRejectedOnTheField(string id, string version, string? url, string field, string code)
    {
        var result = await Service.RegisterModuleAsync(Manifest(id, version, provisionUrl: url));

        Assert.Equal(code, result.Errors[field]);
        Assert.Empty(_audit.Records);
    }

    [Fact]
    public async Task UC15_R3_RequiringItself_IsInvalidValue()
    {
        var result = await Service.RegisterModuleAsync(Manifest("warehouse", requires: ["warehouse"]));

        Assert.Equal("InvalidValue", result.Errors["requires[0]"]);
    }

    [Fact]
    public async Task UC15_S5_CoreCannotBeDeprecated_UnknownModuleIsNotFound()
    {
        Assert.Equal("InvalidValue", (await Service.SetModuleStatusAsync("core", "Deprecated"))!.Errors["status"]);
        Assert.Null(await Service.SetModuleStatusAsync("nope", "Deprecated"));
    }

    [Fact]
    public async Task UC16_S1_PlanAlwaysIncludesCore()
    {
        var result = await Service.SavePlanAsync(null, Plan("pro"));

        Assert.Equal(["core"], _catalog.Plans.Single(p => p.Id == Guid.Parse(result!.Id!)).ModuleIds);
    }

    [Fact]
    public async Task UC16_S3_InvalidValues_AreRejectedOnEachField()
    {
        var result = await Service.SavePlanAsync(null, Plan("pro", price: -1, period: "Weekly", trialDays: 91, maxUsers: 0, modules: ["nope"]));

        Assert.Equal(
            ["billingPeriod", "maxUsers", "moduleIds[0]", "price", "trialDays"],
            result!.Errors.Keys.Order());
    }

    [Fact]
    public async Task UC16_S2_DuplicateCode_IsNotUnique_ButTheSamePlanKeepsIt()
    {
        var id = Guid.Parse((await Service.SavePlanAsync(null, Plan("pro")))!.Id!);

        Assert.Equal("NotUnique", (await Service.SavePlanAsync(null, Plan("pro")))!.Errors["code"]);
        Assert.True((await Service.SavePlanAsync(id, Plan("pro", price: 20)))!.Succeeded);
        Assert.Null(await Service.SavePlanAsync(Guid.NewGuid(), Plan("base")));
    }
}
