using FinHorizon.MySync.Application.Features.Memberships;
using FinHorizon.MySync.Domain.Common;
using FinHorizon.MySync.Domain.Memberships;
using FinHorizon.MySync.Infrastructure.Payments;
using Microsoft.Extensions.Options;
using Stripe;
using Xunit;

namespace FinHorizon.MySync.Infrastructure.Tests;

/// <summary>
/// UC17 against the real Stripe sandbox: the adapter must read what the current Stripe API really returns.
/// Runs only with the variable STRIPE_SANDBOX_KEY (a sk_test_ key); without it the test does nothing, so the suite
/// never needs the network. It publishes a plan the way the Admin Portal does (UC16-R5) for a test module nobody sells,
/// uses a customer without email (Stripe sends nothing) and archives or deletes everything at the end.
/// </summary>
public sealed class StripeSandboxTests
{
    private static readonly string? Key = Environment.GetEnvironmentVariable("STRIPE_SANDBOX_KEY");

    [Fact]
    public async Task UC17_Sandbox_PlansCheckoutPortalAndSnapshots()
    {
        if (string.IsNullOrWhiteSpace(Key) || !Key.StartsWith("sk_test_", StringComparison.Ordinal))
        {
            return;
        }

        var module = $"sandbox-test-{Guid.NewGuid():N}";
        var membership = new MembershipOptions { ModuleId = module, FrontendUrl = new Uri("http://localhost:5174") };
        using var provider = new StripePaymentProvider(Options.Create(new StripeOptions { SecretKey = Key }), Options.Create(membership));
        var client = new StripeClient(Key);
        var (product, taxRate) = await PublishPlanAsync(client, module);
        var owner = new OwnerId(module);
        var customerId = await provider.CreateCustomerAsync(owner, email: null, name: "MySync sandbox test");
        try
        {
            var plan = Assert.Single(await provider.GetPlansAsync());
            Assert.Equal(("sandbox-mensile", 2, 4.99m, "EUR", "Month", true, 22m, 14), (plan.Code, plan.Tier, plan.Price, plan.Currency, plan.Interval, plan.PriceIncludesVat, plan.VatRate, plan.TrialDays));

            var checkout = await provider.CreateCheckoutAsync(customerId, owner, plan, withTrial: true, membership.ReturnUrl("paid"));
            var portal = await provider.CreatePortalAsync(customerId, membership.ReturnUrl("portal"));
            Assert.Equal(("checkout.stripe.com", "billing.stripe.com"), (checkout.Host, portal.Host));
            Assert.Equal(MembershipStatus.Expired, (await provider.GetSnapshotAsync(customerId)).Status);

            // Paid with the test Visa: Active, with the card, the plan and the next renewal.
            await SubscribeWithAsync(client, module, customerId, plan, "pm_card_visa");
            var active = await provider.GetSnapshotAsync(customerId);
            Assert.Equal((MembershipStatus.Active, "visa", "4242"), (active.Status, active.Card?.Brand, active.Card?.Last4));
            Assert.Equal(new SubscribedPlan("MySync sandbox", 4.99m, "EUR", "Month", 2), active.Plan);
            Assert.True(active.CurrentPeriodEnd > DateTimeOffset.UtcNow.AddDays(25));
            Assert.Null(active.LastFailure);

            // A card that is declined: the subscription does not start and the failure is read with its code.
            await new SubscriptionService(client).CancelAsync(active.SubscriptionId!);
            await SubscribeWithAsync(client, module, customerId, plan, "pm_card_chargeCustomerFail");
            var failed = await provider.GetSnapshotAsync(customerId);
            Assert.Equal(MembershipStatus.Expired, failed.Status);
            // The decline code when Stripe gives one (generic_decline for this test card), otherwise the error code.
            Assert.Equal("generic_decline", failed.LastFailure?.Code);
        }
        finally
        {
            await new CustomerService(client).DeleteAsync(customerId);
            await new ProductService(client).UpdateAsync(product, new ProductUpdateOptions { Active = false });
            await new TaxRateService(client).UpdateAsync(taxRate, new TaxRateUpdateOptions { Active = false });
        }
    }

    /// <summary>
    /// UC17 flow 1b against the sandbox: an upgrade is charged at once and applies; when the card refuses the difference,
    /// the subscription stays on its plan and the failure is returned.
    /// </summary>
    [Fact]
    public async Task UC17_Sandbox_UpgradePaidAndRefused()
    {
        if (string.IsNullOrWhiteSpace(Key) || !Key.StartsWith("sk_test_", StringComparison.Ordinal))
        {
            return;
        }

        var module = $"sandbox-test-{Guid.NewGuid():N}";
        var membership = new MembershipOptions { ModuleId = module, FrontendUrl = new Uri("http://localhost:5174") };
        using var provider = new StripePaymentProvider(Options.Create(new StripeOptions { SecretKey = Key }), Options.Create(membership));
        var client = new StripeClient(Key);
        var taxRate = await CreateTaxRateAsync(client);
        var products = new List<string>
        {
            await PublishProductAsync(client, module, taxRate, "sandbox-plus", "MySync Plus", tier: 2, cents: 499),
            await PublishProductAsync(client, module, taxRate, "sandbox-pro", "MySync Pro", tier: 3, cents: 999),
            await PublishProductAsync(client, module, taxRate, "sandbox-top", "MySync Top", tier: 4, cents: 1999),
        };
        var owner = new OwnerId(module);
        var customerId = await provider.CreateCustomerAsync(owner, email: null, name: "MySync sandbox test");
        try
        {
            var plans = (await provider.GetPlansAsync()).ToDictionary(plan => plan.Code);
            await SubscribeWithAsync(client, module, customerId, plans["sandbox-plus"], "pm_card_visa");
            var subscriptionId = (await provider.GetSnapshotAsync(customerId)).SubscriptionId!;

            var upgraded = await provider.ChangePlanAsync(customerId, subscriptionId, plans["sandbox-pro"]);
            var pro = await provider.GetSnapshotAsync(customerId);
            Assert.True(upgraded.Applied);
            Assert.Equal((MembershipStatus.Active, 3, 9.99m), (pro.Status, pro.Plan?.Tier, pro.Plan?.Amount));
            var subscription = await new SubscriptionService(client).GetAsync(subscriptionId);
            Assert.Equal([taxRate], subscription.Items.Data.Single().TaxRates.Select(rate => rate.Id));
            Assert.Equal("sandbox-pro", subscription.Metadata["plan_code"]);

            // The card now refuses: the difference is not paid, the plan stays Pro.
            var declining = await new PaymentMethodService(client).AttachAsync("pm_card_chargeCustomerFail", new PaymentMethodAttachOptions { Customer = customerId });
            await new SubscriptionService(client).UpdateAsync(subscriptionId, new SubscriptionUpdateOptions { DefaultPaymentMethod = declining.Id });
            var refused = await provider.ChangePlanAsync(customerId, subscriptionId, plans["sandbox-top"]);
            var still = await provider.GetSnapshotAsync(customerId);
            Assert.False(refused.Applied);
            Assert.NotNull(refused.Failure?.Code);
            Assert.Equal((MembershipStatus.Active, 3), (still.Status, still.Plan?.Tier));
        }
        finally
        {
            await new CustomerService(client).DeleteAsync(customerId);
            foreach (var product in products)
            {
                await new ProductService(client).UpdateAsync(product, new ProductUpdateOptions { Active = false });
            }

            await new TaxRateService(client).UpdateAsync(taxRate, new TaxRateUpdateOptions { Active = false });
        }
    }

    /// <summary>A plan as the Admin Portal publishes it (UC16-R5): product of the module, default price, VAT 22% included.</summary>
    private static async Task<(string Product, string TaxRate)> PublishPlanAsync(StripeClient client, string module)
    {
        var taxRate = await CreateTaxRateAsync(client);
        var product = await PublishProductAsync(client, module, taxRate, "sandbox-mensile", "MySync sandbox", tier: 2, cents: 499, trialDays: 14);
        return (product, taxRate);
    }

    private static async Task<string> CreateTaxRateAsync(StripeClient client) =>
        (await new TaxRateService(client).CreateAsync(new TaxRateCreateOptions
        {
            DisplayName = "IVA", Percentage = 22, Inclusive = true, Country = "IT", Jurisdiction = "IT", TaxType = "vat",
            Metadata = new Dictionary<string, string> { ["app"] = "finhorizon-sandbox-test" },
        })).Id;

    private static async Task<string> PublishProductAsync(StripeClient client, string module, string taxRate, string code, string name, int tier, long cents, int trialDays = 0) =>
        (await new ProductService(client).CreateAsync(new ProductCreateOptions
        {
            Name = name,
            Metadata = new Dictionary<string, string>
            {
                ["module"] = module, ["plan_code"] = code, ["tax_rate"] = taxRate,
                ["trial_days"] = trialDays.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["tier"] = tier.ToString(System.Globalization.CultureInfo.InvariantCulture),
            },
            DefaultPriceData = new ProductDefaultPriceDataOptions
            {
                Currency = "eur", UnitAmount = cents, TaxBehavior = "inclusive", Recurring = new ProductDefaultPriceDataRecurringOptions { Interval = "month" },
            },
        })).Id;

    private static async Task SubscribeWithAsync(StripeClient client, string module, string customerId, Application.Abstractions.Payments.MembershipPlan plan, string testPaymentMethod)
    {
        var method = await new PaymentMethodService(client).AttachAsync(testPaymentMethod, new PaymentMethodAttachOptions { Customer = customerId });
        await new CustomerService(client).UpdateAsync(customerId, new CustomerUpdateOptions
        {
            InvoiceSettings = new CustomerInvoiceSettingsOptions { DefaultPaymentMethod = method.Id },
        });
        try
        {
            await new SubscriptionService(client).CreateAsync(new SubscriptionCreateOptions
            {
                Customer = customerId,
                Items = [new SubscriptionItemOptions { Price = plan.PriceId, TaxRates = plan.TaxRateId is null ? null : [plan.TaxRateId] }],
                DefaultPaymentMethod = method.Id,
                // As Checkout does: the subscription belongs to the module (FinHorizon.Modules.Billing).
                Metadata = new Dictionary<string, string> { ["module"] = module },
            });
        }
        catch (StripeException exception) when (exception.StripeError?.Code == "card_declined")
        {
            // Stripe may refuse a subscription whose first payment fails; otherwise it stays incomplete.
            // Either way the failure is on the customer.
        }
    }
}
