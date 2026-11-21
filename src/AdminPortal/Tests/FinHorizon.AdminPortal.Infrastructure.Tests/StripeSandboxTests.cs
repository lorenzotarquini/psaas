using Stripe;
using Xunit;
using FinHorizon.AdminPortal.Application.Abstractions.Payments;
using FinHorizon.AdminPortal.Infrastructure.Payments;

namespace FinHorizon.AdminPortal.Infrastructure.Tests;

/// <summary>
/// UC16 against the real Stripe sandbox. Runs only with STRIPE_SANDBOX_KEY (a sk_test_ key), otherwise does nothing.
/// The plan is for a test module nobody sells and is archived at the end.
/// </summary>
public sealed class StripeSandboxTests
{
    private static readonly string? Key = Environment.GetEnvironmentVariable("STRIPE_SANDBOX_KEY");

    [Fact]
    public async Task UC16_Sandbox_PublishRepriceAndRename()
    {
        if (string.IsNullOrWhiteSpace(Key) || !Key.StartsWith("sk_test_", StringComparison.Ordinal))
        {
            return;
        }

        var publisher = new StripePlanPublisher(Key);
        var client = new StripeClient(Key);
        var code = $"sandbox-{Guid.NewGuid():N}"[..30];
        var plan = new PlanPublication(code, "Sandbox mensile", "Piano di prova", $"sandbox-{code}", "Sandbox", 4.99m, "Monthly", 22m, null, true, 14, true, Tier: 2);

        var created = await publisher.PublishAsync(plan, null);
        try
        {
            var product = await new ProductService(client).GetAsync(created.ProductId);
            var price = await new PriceService(client).GetAsync(created.PriceId);
            var taxRate = await new TaxRateService(client).GetAsync(created.TaxRateId);
            Assert.Equal((plan.ModuleId, code, created.TaxRateId, "14", created.PriceId), (product.Metadata["module"], product.Metadata["plan_code"], product.Metadata["tax_rate"], product.Metadata["trial_days"], product.DefaultPriceId));
            Assert.Equal("2", product.Metadata["tier"]);
            Assert.Equal((499L, "eur", "month", "inclusive", $"plan-{code}"), (price.UnitAmount ?? 0, price.Currency, price.Recurring.Interval, price.TaxBehavior, price.LookupKey));
            Assert.Equal((22m, true, "IT"), (taxRate.Percentage, taxRate.Inclusive, taxRate.Country));

            // R3: a new price goes on sale, the old one is kept for the subscribers but not sold.
            var repriced = await publisher.PublishAsync(plan with { Price = 5.99m }, created);
            Assert.NotEqual(created.PriceId, repriced.PriceId);
            Assert.False((await new PriceService(client).GetAsync(created.PriceId)).Active);
            Assert.Equal(599L, (await new PriceService(client).GetAsync(repriced.PriceId)).UnitAmount);
            Assert.Equal(created.TaxRateId, repriced.TaxRateId);

            // Only the name: same price.
            var renamed = await publisher.PublishAsync(plan with { Price = 5.99m, Name = "Sandbox al mese" }, repriced);
            Assert.Equal(repriced.PriceId, renamed.PriceId);
            Assert.Equal("Sandbox al mese", (await new ProductService(client).GetAsync(created.ProductId)).Name);
        }
        finally
        {
            await new ProductService(client).UpdateAsync(created.ProductId, new ProductUpdateOptions { Active = false });
        }
    }
}
