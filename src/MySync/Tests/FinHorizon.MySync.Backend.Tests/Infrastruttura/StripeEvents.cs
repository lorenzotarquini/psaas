using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace FinHorizon.MySync.Backend.Tests.Infrastruttura;

/// <summary>Events in the shape Stripe sends them, signed as Stripe signs them (header Stripe-Signature).</summary>
public static class StripeEvents
{
    public const string Secret = "whsec_test_mysync";

    public static string Event(string type, string customerId, string objectKind = "invoice") => JsonSerializer.Serialize(new
    {
        id = $"evt_{Guid.NewGuid():N}",
        @object = "event",
        api_version = "2025-01-27.acacia",
        created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
        type,
        livemode = false,
        data = new { @object = new { id = $"{objectKind}_test", @object = objectKind, customer = customerId } },
    });

    public static string Signature(string payload, string secret = Secret)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var hash = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes($"{timestamp}.{payload}"));
        return $"t={timestamp},v1={Convert.ToHexStringLower(hash)}";
    }
}
