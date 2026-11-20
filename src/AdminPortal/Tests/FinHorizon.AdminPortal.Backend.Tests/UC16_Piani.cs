using System.Net;
using System.Net.Http.Json;
using FinHorizon.AdminPortal.Backend.Tests.Infrastruttura;
using Xunit;

namespace FinHorizon.AdminPortal.Backend.Tests;

/// <summary>docs/use-cases/UC16-piani.md</summary>
public sealed class UC16_Piani(PostgresFixture database) : AcceptanceTest(database)
{
    [Fact, Trait("Scenario", "UC16-S1")]
    public async Task S1_CreareUnPiano()
    {
        await GivenModuleAsync("warehouse");

        var pro = await GivenPlanAsync("pro", 99, maxUsers: 50, moduleIds: "warehouse");

        Assert.Equal(["warehouse"], pro.ModuleIds);
        Assert.Equal((99m, 50), (pro.Price, pro.MaxUsers));
    }

    [Fact, Trait("Scenario", "UC16-S2")]
    public async Task S2_CodiceDuplicato()
    {
        await GivenPlanAsync("pro");

        var errors = await ErrorsAsync(await Admin.PostAsJsonAsync("/api/plans", new { code = "PRO", name = "Pro 2", price = 10, billingPeriod = "Monthly", moduleIds = new[] { "base" } }));

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

    private static object MySyncMonthly(decimal price = 4.99m, string name = "MySync mensile") =>
        new { code = "mysync-mensile", name, price, billingPeriod = "Monthly", moduleIds = new[] { "mysync" }, vatRate = 22, priceIncludesVat = true };

    [Fact, Trait("Scenario", "UC16-S4")]
    public async Task S4_PubblicatoSuStripe()
    {
        var plan = await ReadAsync<PlanView>(await Admin.PostAsJsonAsync("/api/plans", MySyncMonthly()), HttpStatusCode.Created);

        Assert.True(plan.Published);
        var product = Host.StripePlans.Products[plan.Stripe!.ProductId];
        Assert.Equal(("mysync", "mysync-mensile", 22m, true), (product.ModuleId, product.Code, product.VatRate, product.PriceIncludesVat));
        Assert.Equal((4.99m, "month", true, true), Host.StripePlans.Prices[plan.Stripe.PriceId]);
    }

    [Fact, Trait("Scenario", "UC16-S5")]
    public async Task S5_NuovoPrezzoSuStripe()
    {
        var plan = await ReadAsync<PlanView>(await Admin.PostAsJsonAsync("/api/plans", MySyncMonthly()), HttpStatusCode.Created);

        var repriced = await ReadAsync<PlanView>(await Admin.PutAsJsonAsync($"/api/plans/{plan.Id}", MySyncMonthly(5.99m)), HttpStatusCode.OK);
        var renamed = await ReadAsync<PlanView>(await Admin.PutAsJsonAsync($"/api/plans/{plan.Id}", MySyncMonthly(5.99m, "MySync al mese")), HttpStatusCode.OK);

        Assert.NotEqual(plan.Stripe!.PriceId, repriced.Stripe!.PriceId);
        Assert.Equal((4.99m, false), (Host.StripePlans.Prices[plan.Stripe.PriceId].Amount, Host.StripePlans.Prices[plan.Stripe.PriceId].OnSale));
        Assert.Equal((5.99m, true), (Host.StripePlans.Prices[repriced.Stripe.PriceId].Amount, Host.StripePlans.Prices[repriced.Stripe.PriceId].OnSale));
        Assert.Equal((repriced.Stripe.PriceId, plan.Stripe.ProductId), (renamed.Stripe!.PriceId, renamed.Stripe.ProductId));
        Assert.Equal("MySync al mese", Host.StripePlans.Products[plan.Stripe.ProductId].Name);
    }

    [Fact, Trait("Scenario", "UC16-S6")]
    public async Task S6_UnSoloModulo()
    {
        await GivenModuleAsync("warehouse");

        var none = await ErrorsAsync(await Admin.PostAsJsonAsync("/api/plans", new { code = "x1", name = "X", price = 10, billingPeriod = "Monthly" }));
        var two = await ErrorsAsync(await Admin.PostAsJsonAsync("/api/plans", new { code = "x2", name = "X", price = 10, billingPeriod = "Monthly", moduleIds = new[] { "mysync", "warehouse" } }));

        Assert.Equal(("Required", "InvalidValue"), (none["moduleIds"], two["moduleIds"]));
    }

    [Fact, Trait("Scenario", "UC16-S7")]
    public async Task S7_NaturaIva()
    {
        var noNature = await ErrorsAsync(await Admin.PostAsJsonAsync("/api/plans", new { code = "x1", name = "X", price = 10, billingPeriod = "Monthly", moduleIds = new[] { "mysync" }, vatRate = 0 }));
        var wrongRate = await ErrorsAsync(await Admin.PostAsJsonAsync("/api/plans", new { code = "x2", name = "X", price = 10, billingPeriod = "Monthly", moduleIds = new[] { "mysync" }, vatRate = 7 }));
        var exempt = await ReadAsync<PlanView>(
            await Admin.PostAsJsonAsync("/api/plans", new { code = "x3", name = "X", price = 10, billingPeriod = "Monthly", moduleIds = new[] { "mysync" }, vatRate = 0, vatNature = "N2.1" }),
            HttpStatusCode.Created);

        Assert.Equal(("Required", "InvalidValue"), (noNature["vatNature"], wrongRate["vatRate"]));
        Assert.Equal((0m, "N2.1"), (exempt.VatRate, exempt.VatNature));
    }

    [Fact, Trait("Scenario", "UC16-S8")]
    public async Task S8_StripeNonRisponde()
    {
        Host.StripePlans.Down = true;

        var response = await Admin.PostAsJsonAsync("/api/plans", MySyncMonthly());

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Empty(await GetOkAsync<List<PlanView>>("/api/plans"));
    }
}
