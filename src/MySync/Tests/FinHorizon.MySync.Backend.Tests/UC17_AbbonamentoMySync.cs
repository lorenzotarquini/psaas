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

    public sealed record TierView(int Level, string Name, List<string> Features, Dictionary<string, int> Limits);

    public sealed record PlanView(string Code, string Name, string? Description, TierView? Tier, decimal Price, string Currency, string Interval, bool PriceIncludesVat, decimal? VatRate, int TrialDays, bool CanUpgradeTo = false);

    public sealed record MembershipView(
        string Status,
        bool CanWrite,
        SubscribedPlan? Plan,
        TierView? Tier,
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

    private static Task<HttpResponseMessage> ChangePlanAsync(HttpClient user, string? planCode) =>
        user.PostAsJsonAsync("/api/membership/change", new { planCode });

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
        new Uri("https://invoice.stripe.test/in_1"),
        new SubscribedPlan("MySync mensile", 4.99m, "EUR", "Month", 2));

    [Fact, Trait("Scenario", "UC17-S1")]
    public async Task S1_UtenteEsistenteScaduto()
    {
        var luca = UserClient("Luca", subscribed: false);

        var membership = await MembershipAsync(luca);
        var plans = await GetOkAsync<List<PlanView>>("/api/membership/plans", luca);

        Assert.Equal(("Expired", false, null), (membership.Status, membership.CanWrite, membership.Plan));
        Assert.Equal(("mysync-mensile", 4.99m, "EUR", "Month", true, 22m), (plans[0].Code, plans[0].Price, plans[0].Currency, plans[0].Interval, plans[0].PriceIncludesVat, plans[0].VatRate));
        Assert.Equal(["mysync-mensile", "mysync-annuale"], plans.Select(plan => plan.Code));
        Assert.Equal("Plus", plans[0].Tier!.Name);
        Assert.Equal(["projection"], plans[0].Tier!.Features);
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
        var yearly = new SubscribedPlan("MySync annuale", 49m, "EUR", "Year", 3);
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

        Assert.Equal(new Uri($"{MySyncHost.FrontendUrl}/settings?tab=billing&membership=paid"), Assert.Single(Host.Payments.Checkouts).ReturnUrl);
        Assert.Equal(new Uri($"{MySyncHost.FrontendUrl}/settings?tab=billing&membership=portal"), Assert.Single(Host.Payments.Portals).ReturnUrl);
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

    [Fact, Trait("Scenario", "UC17-S12")]
    public async Task S12_FeatureFuoriDalTier()
    {
        var anna = UserClient("Anna", tier: 1);

        var base1 = await anna.GetAsync("/api/projection");
        var plus = await Mario.GetAsync("/api/projection");

        Assert.Equal((HttpStatusCode.Forbidden, HttpStatusCode.OK), (base1.StatusCode, plus.StatusCode));
        var problem = await base1.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.Equal(("feature-not-in-tier", "projection"), (problem.GetProperty("type").GetString(), problem.GetProperty("feature").GetString()));
        Assert.Equal("Base", (await MembershipAsync(anna)).Tier!.Name);
    }

    [Fact, Trait("Scenario", "UC17-S13")]
    public async Task S13_PianoDiUnLivelloSconosciuto()
    {
        var luca = UserClient("Luca", subscribed: false);

        var errors = await ErrorsAsync(await CheckoutAsync(luca, "mysync-ignoto"));

        Assert.Equal("InvalidValue", errors["planCode"]);
    }

    [Fact, Trait("Scenario", "UC17-S14")]
    public async Task S14_ManifestPubblico()
    {
        var manifest = await ReadAsync<System.Text.Json.JsonElement>(await Anonymous().GetAsync("/api/module/manifest"), HttpStatusCode.OK);

        Assert.Equal("mysync", manifest.GetProperty("id").GetString());
        Assert.Equal(["Base", "Plus", "Pro"], manifest.GetProperty("tiers").EnumerateArray().Select(tier => tier.GetProperty("name").GetString()));
    }

    [Fact, Trait("Scenario", "UC17-S15")]
    public async Task S15_PassareAUnTierSuperiore()
    {
        var plans = await GetOkAsync<List<PlanView>>("/api/membership/plans", Mario);

        Assert.False(plans.Single(plan => plan.Code == "mysync-mensile").CanUpgradeTo);
        Assert.True(plans.Single(plan => plan.Code == "mysync-annuale").CanUpgradeTo);

        var changed = await ReadAsync<MembershipView>(await ChangePlanAsync(Mario, "mysync-annuale"), HttpStatusCode.OK);

        Assert.Equal((CustomerOf("Mario"), SubscriptionOf("Mario"), "mysync-annuale"), Assert.Single(Host.Payments.PlanChanges));
        Assert.Equal("Active", changed.Status);
        Assert.Equal("Pro", changed.Tier!.Name);
        Assert.Equal(3, changed.Plan!.Tier);
        Assert.Equal("Year", changed.Plan.Interval);
        Assert.Equal("Pro", (await MembershipAsync(Mario)).Tier!.Name);
    }

    [Fact, Trait("Scenario", "UC17-S16")]
    public async Task S16_SoloVersoUnTierSuperiore()
    {
        var luca = UserClient("Luca", subscribed: false);

        var sameTier = await ErrorsAsync(await ChangePlanAsync(Mario, "mysync-mensile"));
        var expired = await ErrorsAsync(await ChangePlanAsync(luca, "mysync-annuale"));
        var missing = await ErrorsAsync(await ChangePlanAsync(Mario, null));

        Assert.Equal("NotAnUpgrade", sameTier["planCode"]);
        Assert.Equal("NotActive", expired["membership"]);
        Assert.Equal("Required", missing["planCode"]);
        Assert.Empty(Host.Payments.PlanChanges);
    }

    [Fact, Trait("Scenario", "UC17-S17")]
    public async Task S17_DifferenzaRifiutata()
    {
        Host.Payments.DeclinePlanChange = "card_declined";

        var errors = await ErrorsAsync(await ChangePlanAsync(Mario, "mysync-annuale"));
        var membership = await MembershipAsync(Mario);

        Assert.Equal("PaymentFailed", errors["membership"]);
        Assert.Equal("Plus", membership.Tier!.Name);
        Assert.Equal("card_declined", membership.LastFailure!.Code);
        Assert.Equal("Active", membership.Status);
    }
}
