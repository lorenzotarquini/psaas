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
            Assert.Equal(("sandbox-mensile", 4.99m, "EUR", "Month", true, 22m, 14), (plan.Code, plan.Price, plan.Currency, plan.Interval, plan.PriceIncludesVat, plan.VatRate, plan.TrialDays));

            var checkout = await provider.CreateCheckoutAsync(customerId, owner, plan, withTrial: true, membership.ReturnUrl("paid"));
            var portal = await provider.CreatePortalAsync(customerId, membership.ReturnUrl("portal"));
            Assert.Equal(("checkout.stripe.com", "billing.stripe.com"), (checkout.Host, portal.Host));
            Assert.Equal(MembershipStatus.Expired, (await provider.GetSnapshotAsync(customerId)).Status);

            // Paid with the test Visa: Active, with the card, the plan and the next renewal.
            await SubscribeWithAsync(client, customerId, plan, "pm_card_visa");
            var active = await provider.GetSnapshotAsync(customerId);
            Assert.Equal((MembershipStatus.Active, "visa", "4242"), (active.Status, active.Card?.Brand, active.Card?.Last4));
            Assert.Equal(new SubscribedPlan("MySync sandbox", 4.99m, "EUR", "Month"), active.Plan);
            Assert.True(active.CurrentPeriodEnd > DateTimeOffset.UtcNow.AddDays(25));
            Assert.Null(active.LastFailure);

            // A card that is declined: the subscription does not start and the failure is read with its code.
            await new SubscriptionService(client).CancelAsync(active.SubscriptionId!);
            await SubscribeWithAsync(client, customerId, plan, "pm_card_chargeCustomerFail");
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

    /// <summary>A plan as the Admin Portal publishes it (UC16-R5): product of the module, default price, VAT 22% included.</summary>
    private static async Task<(string Product, string TaxRate)> PublishPlanAsync(StripeClient client, string module)
    {
        var taxRate = await new TaxRateService(client).CreateAsync(new TaxRateCreateOptions
        {
            DisplayName = "IVA", Percentage = 22, Inclusive = true, Country = "IT", Jurisdiction = "IT", TaxType = "vat",
            Metadata = new Dictionary<string, string> { ["app"] = "finhorizon-sandbox-test" },
        });
        var product = await new ProductService(client).CreateAsync(new ProductCreateOptions
        {
            Name = "MySync sandbox",
            Metadata = new Dictionary<string, string> { ["module"] = module, ["plan_code"] = "sandbox-mensile", ["tax_rate"] = taxRate.Id, ["trial_days"] = "14" },
            DefaultPriceData = new ProductDefaultPriceDataOptions
            {
                Currency = "eur", UnitAmount = 499, TaxBehavior = "inclusive", Recurring = new ProductDefaultPriceDataRecurringOptions { Interval = "month" },
            },
        });
        return (product.Id, taxRate.Id);
    }

    private static async Task SubscribeWithAsync(StripeClient client, string customerId, Application.Abstractions.Payments.MembershipPlan plan, string testPaymentMethod)
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
                Metadata = new Dictionary<string, string> { ["app"] = "mysync" },
            });
        }
        catch (StripeException exception) when (exception.StripeError?.Code == "card_declined")
        {
            // Stripe may refuse a subscription whose first payment fails; otherwise it stays incomplete.
            // Either way the failure is on the customer.
        }
    }
}
