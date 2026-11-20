using System.Globalization;
using FinHorizon.MySync.Application.Abstractions.Payments;
using FinHorizon.MySync.Application.Features.Memberships;
using FinHorizon.MySync.Domain.Common;
using FinHorizon.MySync.Domain.Memberships;
using Microsoft.Extensions.Options;
using NLog;
using Stripe;
using Stripe.Checkout;

namespace FinHorizon.MySync.Infrastructure.Payments;

/// <summary>
/// UC17 on Stripe. The plans are the Admin Portal's (UC16): products of the MySync module with their default price
/// and tax rate. The portal configuration is created by MySync the first time it is needed and found again by metadata.
/// </summary>
internal sealed class StripePaymentProvider(IOptions<StripeOptions> stripeOptions, IOptions<MembershipOptions> membershipOptions)
    : IPaymentProvider, IDisposable
{
    private const string App = "mysync";
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private readonly SemaphoreSlim _setup = new(1, 1);
    private string? _portalConfigurationId;
    private (DateTimeOffset At, IReadOnlyList<MembershipPlan> Plans)? _plans;

    private StripeClient? _client;

    private StripeClient Client => _client ??= string.IsNullOrWhiteSpace(stripeOptions.Value.SecretKey)
        ? throw new PaymentProviderUnavailableException("Stripe:SecretKey is not configured.")
        : new StripeClient(stripeOptions.Value.SecretKey);

    public Task<string> CreateCustomerAsync(OwnerId ownerId, string? email, string? name, CancellationToken cancellationToken = default) =>
        CallAsync(async client =>
        {
            var customer = await new CustomerService(client).CreateAsync(
                new CustomerCreateOptions
                {
                    Email = email,
                    Name = name,
                    PreferredLocales = ["it", "en"],
                    Metadata = new Dictionary<string, string> { ["app"] = App, ["keycloak_id"] = ownerId.Value },
                },
                // R2: asked twice at once, Stripe answers with the same customer.
                new RequestOptions { IdempotencyKey = $"{App}-customer-{ownerId.Value}" },
                cancellationToken).ConfigureAwait(false);
            Logger.Info("Stripe customer created for {UserId}", ownerId);
            return customer.Id;
        });

    public Task<IReadOnlyList<MembershipPlan>> GetPlansAsync(CancellationToken cancellationToken = default) =>
        CallAsync<IReadOnlyList<MembershipPlan>>(async client =>
        {
            // A short cache: the settings page and the checkout ask for them a moment apart.
            if (_plans is { } cached && cached.At > DateTimeOffset.UtcNow.AddMinutes(-1))
            {
                return cached.Plans;
            }

            var moduleId = membershipOptions.Value.ModuleId;
            var plans = new List<MembershipPlan>();
            var products = new ProductService(client).ListAutoPagingAsync(
                new ProductListOptions { Active = true, Limit = 100, Expand = ["data.default_price"] },
                cancellationToken: cancellationToken);
            await foreach (var product in products.ConfigureAwait(false))
            {
                // UC16-R5 of the Admin Portal: a plan is a product of the module with its price on sale as default price.
                if (!product.Metadata.TryGetValue("module", out var module) || module != moduleId
                    || !product.Metadata.TryGetValue("plan_code", out var code)
                    || product.DefaultPrice is not { Active: true, UnitAmount: { } amount, Recurring: { } recurring } price)
                {
                    continue;
                }

                product.Metadata.TryGetValue("tax_rate", out var taxRateId);
                product.Metadata.TryGetValue("trial_days", out var trialDays);
                plans.Add(new MembershipPlan(
                    code,
                    product.Name,
                    string.IsNullOrWhiteSpace(product.Description) ? null : product.Description,
                    amount / 100m,
                    price.Currency.ToUpperInvariant(),
                    recurring.Interval == "year" ? "Year" : "Month",
                    price.TaxBehavior != "exclusive",
                    await VatRateAsync(client, taxRateId, cancellationToken).ConfigureAwait(false),
                    int.TryParse(trialDays, NumberStyles.Integer, CultureInfo.InvariantCulture, out var days) ? days : 0,
                    price.Id,
                    string.IsNullOrWhiteSpace(taxRateId) ? null : taxRateId));
            }

            var sorted = plans.OrderBy(plan => plan.Interval == "Year" ? 1 : 0).ThenBy(plan => plan.Price).ToList();
            _plans = (DateTimeOffset.UtcNow, sorted);
            return sorted;
        });

    public Task<Uri> CreateCheckoutAsync(string customerId, OwnerId ownerId, MembershipPlan plan, bool withTrial, Uri returnUrl, CancellationToken cancellationToken = default) =>
        CallAsync(async client =>
        {
            ArgumentNullException.ThrowIfNull(plan);
            ArgumentNullException.ThrowIfNull(returnUrl);
            var session = await new SessionService(client).CreateAsync(
                new SessionCreateOptions
                {
                    Mode = "subscription",
                    Customer = customerId,
                    ClientReferenceId = ownerId.Value,
                    // The VAT of the plan (UC16-R4) stays on the subscription item, so every renewal has it too.
                    LineItems = [new SessionLineItemOptions { Price = plan.PriceId, Quantity = 1, TaxRates = plan.TaxRateId is null ? null : [plan.TaxRateId] }],
                    SubscriptionData = new SessionSubscriptionDataOptions
                    {
                        TrialPeriodDays = withTrial && plan.TrialDays > 0 ? plan.TrialDays : null,
                        Metadata = new Dictionary<string, string> { ["app"] = App, ["keycloak_id"] = ownerId.Value, ["plan_code"] = plan.Code },
                    },
                    SuccessUrl = returnUrl.ToString(),
                    CancelUrl = returnUrl.ToString(),
                    Locale = "auto",
                },
                cancellationToken: cancellationToken).ConfigureAwait(false);
            return new Uri(session.Url);
        });

    public Task<Uri> CreatePortalAsync(string customerId, Uri returnUrl, CancellationToken cancellationToken = default) =>
        CallAsync(async client =>
        {
            ArgumentNullException.ThrowIfNull(returnUrl);
            var configurationId = await PortalConfigurationIdAsync(client, cancellationToken).ConfigureAwait(false);
            var session = await new Stripe.BillingPortal.SessionService(client).CreateAsync(
                new Stripe.BillingPortal.SessionCreateOptions { Customer = customerId, ReturnUrl = returnUrl.ToString(), Configuration = configurationId },
                cancellationToken: cancellationToken).ConfigureAwait(false);
            return new Uri(session.Url);
        });

    public Task<MembershipSnapshot> GetSnapshotAsync(string customerId, CancellationToken cancellationToken = default) =>
        CallAsync(async client =>
        {
            var subscriptions = await new SubscriptionService(client).ListAsync(
                new SubscriptionListOptions
                {
                    Customer = customerId,
                    Status = "all",
                    Limit = 20,
                    Expand = ["data.default_payment_method", "data.latest_invoice"],
                },
                cancellationToken: cancellationToken).ConfigureAwait(false);
            // The running one first; otherwise the latest that ended (its last failure explains why it ended).
            var subscription = subscriptions.Data
                .Where(item => item.Metadata.TryGetValue("app", out var app) && app == App)
                .OrderBy(item => StatusOf(item.Status) == MembershipStatus.Expired ? 1 : 0)
                .ThenByDescending(item => item.Created)
                .FirstOrDefault();
            if (subscription is null)
            {
                return MembershipSnapshot.None;
            }

            var status = StatusOf(subscription.Status);
            var plan = await SubscribedPlanAsync(client, subscription, cancellationToken).ConfigureAwait(false);
            var paymentMethod = subscription.DefaultPaymentMethod ?? await CustomerPaymentMethodAsync(client, customerId, cancellationToken).ConfigureAwait(false);
            var invoice = subscription.LatestInvoice;
            return new MembershipSnapshot(
                status,
                subscription.Id,
                subscription.Items.Data.FirstOrDefault()?.CurrentPeriodEnd is { } end ? ToUtc(end) : null,
                subscription.CancelAtPeriodEnd,
                paymentMethod?.Card is { } card ? new PaymentCard(card.Brand, card.Last4, (int)card.ExpMonth, (int)card.ExpYear) : null,
                await LastFailureAsync(client, customerId, cancellationToken).ConfigureAwait(false),
                status == MembershipStatus.PastDue && invoice is { Status: "open", HostedInvoiceUrl: { } url } ? new Uri(url) : null,
                plan);
        });

    public void Dispose() => _setup.Dispose();

    /// <summary>UC17-R3.</summary>
    internal static MembershipStatus StatusOf(string status) => status switch
    {
        "active" or "trialing" => MembershipStatus.Active,
        "past_due" => MembershipStatus.PastDue,
        _ => MembershipStatus.Expired,
    };

    /// <summary>The plan the subscription pays: the name of its product and its price, as when the user subscribed.</summary>
    private static async Task<SubscribedPlan?> SubscribedPlanAsync(StripeClient client, Subscription subscription, CancellationToken cancellationToken)
    {
        if (subscription.Items.Data.FirstOrDefault()?.Price is not { UnitAmount: { } amount } price)
        {
            return null;
        }

        var product = await new ProductService(client).GetAsync(price.ProductId, cancellationToken: cancellationToken).ConfigureAwait(false);
        return new SubscribedPlan(product.Name, amount / 100m, price.Currency.ToUpperInvariant(), price.Recurring?.Interval == "year" ? "Year" : "Month");
    }

    /// <summary>The percentage of the plan's tax rate, to show "IVA 22% inclusa" before paying.</summary>
    private static async Task<decimal?> VatRateAsync(StripeClient client, string? taxRateId, CancellationToken cancellationToken) =>
        string.IsNullOrWhiteSpace(taxRateId)
            ? null
            : (await new TaxRateService(client).GetAsync(taxRateId, cancellationToken: cancellationToken).ConfigureAwait(false)).Percentage;

    private static DateTimeOffset ToUtc(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));

    private static async Task<PaymentMethod?> CustomerPaymentMethodAsync(StripeClient client, string customerId, CancellationToken cancellationToken)
    {
        var customer = await new CustomerService(client).GetAsync(
            customerId,
            new CustomerGetOptions { Expand = ["invoice_settings.default_payment_method"] },
            cancellationToken: cancellationToken).ConfigureAwait(false);
        return customer.InvoiceSettings?.DefaultPaymentMethod;
    }

    /// <summary>The latest charge attempt of the customer, when it failed: Stripe's decline code and message.</summary>
    private static async Task<PaymentFailure?> LastFailureAsync(StripeClient client, string customerId, CancellationToken cancellationToken)
    {
        var intents = await new PaymentIntentService(client).ListAsync(
            new PaymentIntentListOptions { Customer = customerId, Limit = 1 },
            cancellationToken: cancellationToken).ConfigureAwait(false);
        return intents.Data.FirstOrDefault() is { Status: not "succeeded", LastPaymentError: { } error } intent
            ? new PaymentFailure(error.DeclineCode ?? error.Code, error.Message, ToUtc(intent.Created))
            : null;
    }

    private async Task<string> PortalConfigurationIdAsync(StripeClient client, CancellationToken cancellationToken)
    {
        if (_portalConfigurationId is not null)
        {
            return _portalConfigurationId;
        }

        await _setup.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_portalConfigurationId is not null)
            {
                return _portalConfigurationId;
            }

            var configurations = new Stripe.BillingPortal.ConfigurationService(client);
            var existing = await configurations.ListAsync(new Stripe.BillingPortal.ConfigurationListOptions { Active = true, Limit = 100 }, cancellationToken: cancellationToken).ConfigureAwait(false);
            if (existing.Data.FirstOrDefault(item => item.Metadata.TryGetValue("app", out var app) && app == App) is { } configuration)
            {
                return _portalConfigurationId = configuration.Id;
            }

            var created = await configurations.CreateAsync(
                new Stripe.BillingPortal.ConfigurationCreateOptions
                {
                    BusinessProfile = new Stripe.BillingPortal.ConfigurationBusinessProfileOptions { Headline = "MySync" },
                    Features = new Stripe.BillingPortal.ConfigurationFeaturesOptions
                    {
                        PaymentMethodUpdate = new Stripe.BillingPortal.ConfigurationFeaturesPaymentMethodUpdateOptions { Enabled = true },
                        InvoiceHistory = new Stripe.BillingPortal.ConfigurationFeaturesInvoiceHistoryOptions { Enabled = true },
                        SubscriptionCancel = new Stripe.BillingPortal.ConfigurationFeaturesSubscriptionCancelOptions { Enabled = true, Mode = "at_period_end" },
                    },
                    Metadata = new Dictionary<string, string> { ["app"] = App },
                },
                cancellationToken: cancellationToken).ConfigureAwait(false);
            Logger.Info("Stripe customer portal configuration created");
            return _portalConfigurationId = created.Id;
        }
        finally
        {
            _setup.Release();
        }
    }

    /// <summary>Stripe errors become 503 for the user (UC17): the cause stays in the log.</summary>
    private async Task<T> CallAsync<T>(Func<StripeClient, Task<T>> call)
    {
        var client = Client;
        try
        {
            return await call(client).ConfigureAwait(false);
        }
        catch (StripeException exception)
        {
            Logger.Error(exception, "Stripe call failed: {StripeError}", exception.StripeError?.Code ?? exception.Message);
            throw new PaymentProviderUnavailableException("Stripe call failed.", exception);
        }
    }
}
