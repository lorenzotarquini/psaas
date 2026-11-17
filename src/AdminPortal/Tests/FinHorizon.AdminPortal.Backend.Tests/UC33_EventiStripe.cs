using System.Net;
using System.Text;
using FinHorizon.AdminPortal.Backend.Tests.Infrastruttura;
using Xunit;

namespace FinHorizon.AdminPortal.Backend.Tests;

/// <summary>docs/use-cases/UC33-eventi-stripe.md</summary>
public sealed class UC33_EventiStripe(PostgresFixture database) : AcceptanceTest(database)
{
    private readonly PostgresFixture _database = database;

    /// <summary>Stripe has no token: the call is anonymous, the signature is the only proof (UC33-R1).</summary>
    private static Task<HttpResponseMessage> SendAsync(HttpClient stripe, string payload, string? signature)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/stripe/webhook")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json"),
        };
        if (signature is not null)
        {
            request.Headers.TryAddWithoutValidation("Stripe-Signature", signature);
        }

        return stripe.SendAsync(request);
    }

    private Task<HttpResponseMessage> StripeSendsAsync(string payload) =>
        SendAsync(Anonymous(), payload, StripeEvents.Signature(payload));

    private Task<List<PaymentEventView>> ReceivedAsync() => GetOkAsync<List<PaymentEventView>>("/api/stripe/events");

    [Fact, Trait("Scenario", "UC33-S1")]
    public async Task S1_EventoFirmato()
    {
        var response = await StripeSendsAsync(StripeEvents.Event("invoice.paid"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("invoice.paid", Assert.Single(await ReceivedAsync()).Type);
    }

    [Fact, Trait("Scenario", "UC33-S2")]
    public async Task S2_FirmaNonValida()
    {
        var payload = StripeEvents.Event("invoice.paid");

        var unsigned = await SendAsync(Anonymous(), payload, signature: null);
        var otherSecret = await SendAsync(Anonymous(), payload, StripeEvents.Signature(payload, "whsec_someone_else"));
        var tampered = await SendAsync(Anonymous(), payload.Replace("invoice.paid", "invoice.voided"), StripeEvents.Signature(payload));

        Assert.Equal(
            (HttpStatusCode.BadRequest, HttpStatusCode.BadRequest, HttpStatusCode.BadRequest),
            (unsigned.StatusCode, otherSecret.StatusCode, tampered.StatusCode));
        Assert.Empty(await ReceivedAsync());
    }

    [Fact, Trait("Scenario", "UC33-S3")]
    public async Task S3_FirmaScaduta()
    {
        var payload = StripeEvents.Event("invoice.paid");

        var response = await SendAsync(Anonymous(), payload, StripeEvents.Signature(payload, signedAt: DateTimeOffset.UtcNow.AddMinutes(-10)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(await ReceivedAsync());
    }

    [Fact, Trait("Scenario", "UC33-S4")]
    public async Task S4_EventoRipetuto()
    {
        var payload = StripeEvents.Event("invoice.paid");

        // Twice at the same time as well: the database key decides (UC33-R2).
        var responses = await Task.WhenAll(StripeSendsAsync(payload), StripeSendsAsync(payload), StripeSendsAsync(payload));

        Assert.All(responses, response => Assert.Equal(HttpStatusCode.OK, response.StatusCode));
        Assert.Single(await ReceivedAsync());
    }

    [Fact, Trait("Scenario", "UC33-S5")]
    public async Task S5_SegretoMancante()
    {
        await using var unconfigured = new AdminPortalHost(_database.ConnectionString) { StripeWebhookSecret = "" };
        var payload = StripeEvents.Event("invoice.paid");

        var response = await SendAsync(unconfigured.CreateClient(), payload, StripeEvents.Signature(payload));

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Empty(await ReceivedAsync());
    }

    [Fact, Trait("Scenario", "UC33-S6")]
    public async Task S6_SoloAmministratori()
    {
        var response = await Anonymous().GetAsync("/api/stripe/events");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
