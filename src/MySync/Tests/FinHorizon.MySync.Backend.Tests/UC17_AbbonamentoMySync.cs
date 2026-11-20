using System.Net;
using System.Net.Http.Json;
using System.Text;
using FinHorizon.MySync.Backend.Tests.Infrastruttura;
using FinHorizon.MySync.Domain.Memberships;
using Xunit;

namespace FinHorizon.MySync.Backend.Tests;

/// <summary>docs/mysync/use-cases/UC17-abbonamento-mysync.md</summary>
public sealed class UC17_AbbonamentoMySync(PostgresFixture database) : AcceptanceTest(database)
{
    private static readonly DateTimeOffset Renewal = new(2026, 12, 15, 9, 0, 0, TimeSpan.Zero);
    private static readonly PaymentCard Visa = new("visa", "4242", 12, 2030);

    public sealed record FailureView(string? Code, string? Message, DateTimeOffset FailedAt);

    public sealed record PlanView(string Code, string Name, string? Description, decimal Price, string Currency, string Interval, bool PriceIncludesVat, decimal? VatRate, int TrialDays);

    public sealed record MembershipView(
        string Status,
        bool CanWrite,
        SubscribedPlan? Plan,
        DateTimeOffset? CurrentPeriodEnd,
        bool CancelAtPeriodEnd,
        PaymentCard? Card,
        FailureView? LastFailure,
        Uri? PayUrl,
        bool HasCustomer);

    public sealed record RedirectView(Uri Url);

    private Task<MembershipView> MembershipAsync(HttpClient user) => GetOkAsync<MembershipView>("/api/membership", user);

    private static Task<HttpResponseMessage> CheckoutAsync(HttpClient user, string? planCode = "mysync-mensile") =>
        user.PostAsJsonAsync("/api/membership/checkout", new { planCode });

    private static Task<HttpResponseMessage> CreateAccountAsync(HttpClient user) =>
        user.PostAsJsonAsync("/api/accounts", AccountBody("Conto corrente", 100m));

    private Task<HttpResponseMessage> StripeSendsAsync(string payload, string? signature) =>
        Anonymous().SendAsync(new HttpRequestMessage(HttpMethod.Post, "/api/membership/webhook")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json"),
            Headers = { { "Stripe-Signature", signature ?? string.Empty } },
        });

    private async Task StripeSaysAsync(string customerId, MembershipSnapshot snapshot, string eventType)
    {
        Host.Payments.Snapshots[customerId] = snapshot;
        var payload = StripeEvents.Event(eventType, customerId);
        Assert.Equal(HttpStatusCode.OK, (await StripeSendsAsync(payload, StripeEvents.Signature(payload))).StatusCode);
    }

    private static MembershipSnapshot PastDue => new(
        MembershipStatus.PastDue,
        "sub_1",
        Renewal,
        false,
        Visa,
        new PaymentFailure("insufficient_funds", "Your card has insufficient funds.", new DateTimeOffset(2026, 11, 15, 9, 0, 0, TimeSpan.Zero)),
        new Uri("https://invoice.stripe.test/in_1"));

    [Fact, Trait("Scenario", "UC17-S1")]
    public async Task S1_UtenteEsistenteScaduto()
    {
        var luca = UserClient("Luca", subscribed: false);

        var membership = await MembershipAsync(luca);
        var plans = await GetOkAsync<List<PlanView>>("/api/membership/plans", luca);

        Assert.Equal(("Expired", false, null), (membership.Status, membership.CanWrite, membership.Plan));
        Assert.Equal(("mysync-mensile", 4.99m, "EUR", "Month", true, 22m), (plans[0].Code, plans[0].Price, plans[0].Currency, plans[0].Interval, plans[0].PriceIncludesVat, plans[0].VatRate));
        Assert.Equal(HttpStatusCode.OK, (await luca.GetAsync("/api/accounts")).StatusCode);
        Assert.Equal(HttpStatusCode.PaymentRequired, (await CreateAccountAsync(luca)).StatusCode);
    }

    [Fact, Trait("Scenario", "UC17-S2")]
    public async Task S2_RinnovoConCheckout()
    {
        var luca = UserClient("Luca", subscribed: false);

        var checkout = await ReadAsync<RedirectView>(await CheckoutAsync(luca, "mysync-annuale"), HttpStatusCode.OK);

        var customer = Assert.Single(Host.Payments.Customers);
        Assert.Equal(("mysync-annuale", false), (Host.Payments.Checkouts.Single().PlanCode, Host.Payments.Checkouts.Single().WithTrial));
        Assert.Equal(("kc-luca", "luca@example.com"), (customer.Owner.Value, customer.Email));
        Assert.Equal(new Uri($"https://checkout.stripe.test/{customer.CustomerId}"), checkout.Url);

        // Paid on Stripe's page: back in MySync the state is read at once.
        var yearly = new SubscribedPlan("MySync annuale", 49m, "EUR", "Year");
        Host.Payments.Snapshots[customer.CustomerId] = new MembershipSnapshot(MembershipStatus.Active, "sub_1", Renewal, false, Visa, null, null, yearly);
        var membership = await ReadAsync<MembershipView>(await luca.PostAsync("/api/membership/refresh", null), HttpStatusCode.OK);

        Assert.Equal(("Active", true, Visa, Renewal, yearly), (membership.Status, membership.CanWrite, membership.Card, membership.CurrentPeriodEnd, membership.Plan));
        Assert.Equal(HttpStatusCode.Created, (await CreateAccountAsync(luca)).StatusCode);
    }

    [Fact, Trait("Scenario", "UC17-S3")]
    public async Task S3_PagamentoNonRiuscito()
    {
        await StripeSaysAsync(CustomerOf("Mario"), PastDue, "invoice.payment_failed");

        var membership = await MembershipAsync(Mario);

        Assert.Equal(("PastDue", true, "insufficient_funds"), (membership.Status, membership.CanWrite, membership.LastFailure?.Code));
        Assert.Equal(new Uri("https://invoice.stripe.test/in_1"), membership.PayUrl);
        Assert.Equal(HttpStatusCode.Created, (await CreateAccountAsync(Mario)).StatusCode);
    }

    [Fact, Trait("Scenario", "UC17-S4")]
    public async Task S4_TentativiEsauriti()
    {
        await StripeSaysAsync(CustomerOf("Mario"), PastDue, "invoice.payment_failed");

        await StripeSaysAsync(CustomerOf("Mario"), PastDue with { Status = MembershipStatus.Expired, PayUrl = null }, "customer.subscription.deleted");

        var membership = await MembershipAsync(Mario);
        Assert.Equal(("Expired", false, "insufficient_funds"), (membership.Status, membership.CanWrite, membership.LastFailure?.Code));
        Assert.Equal(HttpStatusCode.PaymentRequired, (await CreateAccountAsync(Mario)).StatusCode);
    }

    [Fact, Trait("Scenario", "UC17-S5")]
    public async Task S5_GiaAttivo()
    {
        var checkout = await CheckoutAsync(Mario);

        Assert.Equal("AlreadyActive", (await ErrorsAsync(checkout))["membership"]);
        var portal = await ReadAsync<RedirectView>(await Mario.PostAsync("/api/membership/portal", null), HttpStatusCode.OK);
        Assert.Equal(new Uri($"https://billing.stripe.test/{CustomerOf("Mario")}"), portal.Url);
        Assert.Empty(Host.Payments.Checkouts);
    }

    [Fact, Trait("Scenario", "UC17-S6")]
    public async Task S6_RitornoSuMySync()
    {
        var luca = UserClient("Luca", subscribed: false);

        await ReadAsync<RedirectView>(await CheckoutAsync(luca), HttpStatusCode.OK);
        await ReadAsync<RedirectView>(await luca.PostAsync("/api/membership/portal", null), HttpStatusCode.OK);

        Assert.Equal(new Uri($"{MySyncHost.FrontendUrl}/settings?membership=paid"), Assert.Single(Host.Payments.Checkouts).ReturnUrl);
        Assert.Equal(new Uri($"{MySyncHost.FrontendUrl}/settings?membership=portal"), Assert.Single(Host.Payments.Portals).ReturnUrl);
    }

    [Fact, Trait("Scenario", "UC17-S7")]
    public async Task S7_SoloUtentiFinali()
    {
        var notAnEndUser = ClientWith(KeycloakTokens.Create("Luca", [KeycloakTokens.MySyncRole]));

        var response = await CheckoutAsync(notAnEndUser);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Empty(Host.Payments.Customers);
    }

    [Fact, Trait("Scenario", "UC17-S8")]
    public async Task S8_EventoNonFirmato()
    {
        var payload = StripeEvents.Event("customer.subscription.deleted", CustomerOf("Mario"));

        var unsigned = await StripeSendsAsync(payload, signature: null);
        var otherSecret = await StripeSendsAsync(payload, StripeEvents.Signature(payload, "whsec_someone_else"));

        Assert.Equal((HttpStatusCode.BadRequest, HttpStatusCode.BadRequest), (unsigned.StatusCode, otherSecret.StatusCode));
        Assert.Equal(0, Host.Payments.SnapshotCalls);
        Assert.Equal("Active", (await MembershipAsync(Mario)).Status);
    }

    [Fact, Trait("Scenario", "UC17-S9")]
    public async Task S9_ClientiDiAltri()
    {
        var payload = StripeEvents.Event("invoice.paid", "cus_organization");

        var response = await StripeSendsAsync(payload, StripeEvents.Signature(payload));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(0, Host.Payments.SnapshotCalls);
    }

    [Fact, Trait("Scenario", "UC17-S10")]
    public async Task S10_SoloPianiInVendita()
    {
        var luca = UserClient("Luca", subscribed: false);

        var archived = await ErrorsAsync(await CheckoutAsync(luca, "mysync-vecchio"));
        var none = await ErrorsAsync(await CheckoutAsync(luca, planCode: null));

        Assert.Equal(("InvalidValue", "Required"), (archived["planCode"], none["planCode"]));
        Assert.Empty(Host.Payments.Customers);
    }

    [Fact, Trait("Scenario", "UC17-S11")]
    public async Task S11_ProvaSoloLaPrimaVolta()
    {
        var luca = UserClient("Luca", subscribed: false);

        await CheckoutAsync(luca);
        await CheckoutAsync(luca);

        Assert.Equal([true, false], Host.Payments.Checkouts.Select(checkout => checkout.WithTrial));
    }
}
