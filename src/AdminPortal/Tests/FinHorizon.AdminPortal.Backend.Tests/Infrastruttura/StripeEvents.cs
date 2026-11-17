using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace FinHorizon.AdminPortal.Backend.Tests.Infrastruttura;

/// <summary>
/// Stripe simulated (UC33): events in the shape Stripe sends them, signed as Stripe signs them
/// (header Stripe-Signature "t=&lt;unix time&gt;,v1=&lt;HMAC-SHA256 of "t.payload" with the endpoint secret&gt;").
/// </summary>
public static class StripeEvents
{
    public const string Secret = "whsec_test_finhorizon";

    public static string Event(string type, string? id = null) => JsonSerializer.Serialize(new
    {
        id = id ?? $"evt_{Guid.NewGuid():N}",
        @object = "event",
        api_version = "2025-01-27.acacia",
        created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
        type,
        livemode = false,
        pending_webhooks = 1,
        data = new { @object = new { id = "in_test", @object = "invoice" } },
        request = new { id = (string?)null, idempotency_key = (string?)null },
    });

    /// <summary>The Stripe-Signature header; real time, since the backend checks it against its own clock.</summary>
    public static string Signature(string payload, string secret = Secret, DateTimeOffset? signedAt = null)
    {
        var timestamp = (signedAt ?? DateTimeOffset.UtcNow).ToUnixTimeSeconds();
        var hash = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes($"{timestamp}.{payload}"));
        return $"t={timestamp},v1={Convert.ToHexStringLower(hash)}";
    }
}
