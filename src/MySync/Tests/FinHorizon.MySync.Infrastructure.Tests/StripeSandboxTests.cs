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
/// never needs the network. The customer it creates has no email (Stripe sends nothing) and is deleted at the end.
/// </summary>
public sealed class StripeSandboxTests
{
    private static readonly string? Key = Environment.GetEnvironmentVariable("STRIPE_SANDBOX_KEY");

    [Fact]
    public async Task UC17_Sandbox_CustomerCheckoutPortalAndSnapshots()
    {
        if (string.IsNullOrWhiteSpace(Key) || !Key.StartsWith("sk_test_", StringComparison.Ordinal))
        {
            return;
        }

        var membership = new MembershipOptions { Price = 4.99m, Currency = "EUR", FrontendUrl = new Uri("http://localhost:5174") };
        using var provider = new StripePaymentProvider(Options.Create(new StripeOptions { SecretKey = Key }), Options.Create(membership));
        var client = new StripeClient(Key);
        var owner = new OwnerId($"sandbox-test-{Guid.NewGuid():N}");
        var customerId = await provider.CreateCustomerAsync(owner, email: null, name: "MySync sandbox test");
        try
        {
            var checkout = await provider.CreateCheckoutAsync(customerId, owner, membership.ReturnUrl("paid"));
            var portal = await provider.CreatePortalAsync(customerId, membership.ReturnUrl("portal"));
            Assert.Equal(("checkout.stripe.com", "billing.stripe.com"), (checkout.Host, portal.Host));
            Assert.Equal(MembershipStatus.Expired, (await provider.GetSnapshotAsync(customerId)).Status);

            var price = (await new PriceService(client).ListAsync(new PriceListOptions { LookupKeys = ["mysync-month-499-eur"] })).Data.Single();
            Assert.Equal((499L, "eur", "month"), (price.UnitAmount ?? 0, price.Currency, price.Recurring.Interval));

            // Paid with the test Visa: Active, with the card and the next renewal.
            await SubscribeWithAsync(client, customerId, price.Id, "pm_card_visa");
            var active = await provider.GetSnapshotAsync(customerId);
            Assert.Equal((MembershipStatus.Active, "visa", "4242"), (active.Status, active.Card?.Brand, active.Card?.Last4));
            Assert.True(active.CurrentPeriodEnd > DateTimeOffset.UtcNow.AddDays(25));
            Assert.Null(active.LastFailure);

            // A card that is declined: the subscription does not start and the failure is read with its code.
            await new SubscriptionService(client).CancelAsync(active.SubscriptionId!);
            await SubscribeWithAsync(client, customerId, price.Id, "pm_card_chargeCustomerFail");
            var failed = await provider.GetSnapshotAsync(customerId);
            Assert.Equal(MembershipStatus.Expired, failed.Status);
            // The decline code when Stripe gives one (generic_decline for this test card), otherwise the error code.
            Assert.Equal("generic_decline", failed.LastFailure?.Code);
        }
        finally
        {
            await new CustomerService(client).DeleteAsync(customerId);
        }
    }

    private static async Task SubscribeWithAsync(StripeClient client, string customerId, string priceId, string testPaymentMethod)
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
                Items = [new SubscriptionItemOptions { Price = priceId }],
                DefaultPaymentMethod = method.Id,
                Metadata = new Dictionary<string, string> { ["app"] = "mysync" },
            });
        }
        catch (StripeException exception) when (exception.StripeError?.Code == "card_declined")
        {
            // Stripe refuses a subscription whose first payment fails with error_if_incomplete; the default
            // (allow_incomplete) leaves it incomplete instead. Either way the failure is on the customer.
        }
    }
}
