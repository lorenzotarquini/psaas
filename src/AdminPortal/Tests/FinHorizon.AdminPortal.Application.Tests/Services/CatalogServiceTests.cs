using FinHorizon.Modules.Sdk;
using FinHorizon.AdminPortal.Application.Contracts.Catalog;
using FinHorizon.AdminPortal.Application.Services.Catalog;
using Xunit;

namespace FinHorizon.AdminPortal.Application.Tests.Services;

/// <summary>Derived from UC15 (catalogo moduli) and UC16 (piani).</summary>
public sealed class CatalogServiceTests
{
    private readonly FakeCatalogRepository _catalog = new();
    private readonly FakeAuditLog _audit = new();

    private readonly FakePlanPublisher _stripe = new();

    public CatalogServiceTests() => _catalog.Modules.Add(FakeCatalogRepository.Module("mysync"));

    private CatalogService Service => new(_catalog, _audit, _stripe);

    private static ModuleManifest Manifest(string id, string version = "1.0.0", string[]? requires = null, string? provisionUrl = null) =>
        new(id, id, null, version, null, null, requires, null, provisionUrl, null);

    private static PlanRequest Plan(
        string code, decimal? price = 10, string? period = "Monthly", int? trialDays = 0, int? maxUsers = null, string[]? modules = null,
        decimal? vatRate = null, string? vatNature = null) =>
        new(code, code, null, price, period, trialDays, maxUsers, null, modules ?? ["mysync"], VatRate: vatRate, VatNature: vatNature);

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
    public async Task UC15_S5_AnyModuleCanBeDeprecated_UnknownModuleIsNotFound()
    {
        _catalog.Modules.Add(FakeCatalogRepository.Module("mysync"));

        Assert.Empty((await Service.SetModuleStatusAsync("mysync", "Deprecated"))!.Errors);
        Assert.Null(await Service.SetModuleStatusAsync("nope", "Deprecated"));
    }

    [Fact]
    public async Task UC16_S1_PlanHasItsModule_PublishedWithDefaultVat()
    {
        var result = await Service.SavePlanAsync(null, Plan("pro"));

        var plan = _catalog.Plans.Single(p => p.Id == Guid.Parse(result!.Id!));
        Assert.Equal(["mysync"], plan.ModuleIds);
        Assert.Equal((22m, true, true), (plan.VatRate, plan.PriceIncludesVat, plan.Published));
        Assert.Equal(("mysync", 10m), (_stripe.Published.Single().ModuleId, _stripe.Published.Single().Price));
    }

    [Theory]
    [InlineData(new string[0], "moduleIds", "Required")]
    [InlineData(new[] { "mysync", "other" }, "moduleIds", "InvalidValue")]
    public async Task UC16_R2_OneModulePerPlan(string[] modules, string field, string code)
    {
        _catalog.Modules.Add(FakeCatalogRepository.Module("other"));

        var result = await Service.SavePlanAsync(null, Plan("pro", modules: modules));

        Assert.Equal(code, result!.Errors[field]);
        Assert.Empty(_stripe.Published);
    }

    [Theory]
    [InlineData(0, null, "vatNature", "Required")]
    [InlineData(0, "N9", "vatNature", "InvalidValue")]
    [InlineData(22, "N2.1", "vatNature", "InvalidValue")]
    [InlineData(7, null, "vatRate", "InvalidValue")]
    public async Task UC16_R4_VatRateAndNature(int rate, string? nature, string field, string code)
    {
        var result = await Service.SavePlanAsync(null, Plan("pro", vatRate: rate, vatNature: nature));

        Assert.Equal(code, result!.Errors[field]);
    }

    [Fact]
    public async Task UC16_R4_ZeroVatWithNature_IsAccepted()
    {
        var result = await Service.SavePlanAsync(null, Plan("pro", vatRate: 0, vatNature: "n2.1"));

        Assert.True(result!.Succeeded);
        Assert.Equal("N2.1", _stripe.Published.Single().VatNature);
    }

    [Fact]
    public async Task UC16_2b_StripeDown_NothingSaved()
    {
        _stripe.Unavailable = true;

        await Assert.ThrowsAsync<FinHorizon.AdminPortal.Application.Abstractions.Payments.PaymentProviderUnavailableException>(() => Service.SavePlanAsync(null, Plan("pro")));

        Assert.Empty(_catalog.Plans);
        Assert.Empty(_audit.Records);
    }

    [Fact]
    public async Task UC16_2c_WithoutStripe_SavedUnpublished()
    {
        _stripe.IsConfigured = false;

        var result = await Service.SavePlanAsync(null, Plan("pro"));

        Assert.False(_catalog.Plans.Single(p => p.Id == Guid.Parse(result!.Id!)).Published);
        Assert.Empty(_stripe.Published);
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
