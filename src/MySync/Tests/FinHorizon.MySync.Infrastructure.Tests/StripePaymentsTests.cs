using System.Security.Cryptography;
using System.Text;
using FinHorizon.MySync.Domain.Memberships;
using FinHorizon.MySync.Infrastructure.Payments;
using Microsoft.Extensions.Options;
using Xunit;

namespace FinHorizon.MySync.Infrastructure.Tests;

/// <summary>UC17: what MySync reads of Stripe's statuses and events.</summary>
public sealed class StripePaymentsTests
{
    private const string Secret = "whsec_unit";

    private static StripePaymentWebhook Webhook(string? secret = Secret) => new(Options.Create(new StripeOptions { WebhookSecret = secret }));

    private static string Sign(string payload)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var hash = HMACSHA256.HashData(Encoding.UTF8.GetBytes(Secret), Encoding.UTF8.GetBytes($"{timestamp}.{payload}"));
        return $"t={timestamp},v1={Convert.ToHexStringLower(hash)}";
    }

    [Theory]
    [InlineData("active", MembershipStatus.Active)]
    [InlineData("trialing", MembershipStatus.Active)]
    [InlineData("past_due", MembershipStatus.PastDue)]
    [InlineData("unpaid", MembershipStatus.Expired)]
    [InlineData("canceled", MembershipStatus.Expired)]
    [InlineData("incomplete", MembershipStatus.Expired)]
    [InlineData("incomplete_expired", MembershipStatus.Expired)]
    [InlineData("paused", MembershipStatus.Expired)]
    public void UC17_R3_StripeStatuses(string stripeStatus, MembershipStatus expected) =>
        Assert.Equal(expected, StripePaymentProvider.StatusOf(stripeStatus));

    [Theory]
    [InlineData("""{"id":"evt_1","type":"invoice.payment_failed","data":{"object":{"object":"invoice","id":"in_1","customer":"cus_1"}}}""", "cus_1")]
    [InlineData("""{"id":"evt_2","type":"customer.updated","data":{"object":{"object":"customer","id":"cus_2"}}}""", "cus_2")]
    [InlineData("""{"id":"evt_3","type":"product.created","data":{"object":{"object":"product","id":"prod_1"}}}""", null)]
    public void UC17_R5_EventGivesItsCustomer(string payload, string? customerId)
    {
        var notification = Webhook().Verify(payload, Sign(payload));

        Assert.NotNull(notification);
        Assert.Equal(customerId, notification.CustomerId);
    }

    [Fact]
    public void UC17_S8_WrongSignatureOrNoSecret()
    {
        const string payload = """{"id":"evt_1","type":"invoice.paid","data":{"object":{"object":"invoice","customer":"cus_1"}}}""";

        Assert.Null(Webhook().Verify(payload, "t=1,v1=00"));
        Assert.Null(Webhook().Verify(payload.Replace("cus_1", "cus_2", StringComparison.Ordinal), Sign(payload)));
        Assert.False(Webhook(secret: null).IsConfigured);
    }
}
