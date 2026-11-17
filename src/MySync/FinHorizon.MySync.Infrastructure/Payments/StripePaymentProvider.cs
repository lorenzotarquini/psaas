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
/// UC17 on Stripe. Product, price and portal configuration are created by MySync the first time they are needed
/// (nothing to set up by hand in the Dashboard) and found again by lookup key or metadata.
/// </summary>
internal sealed class StripePaymentProvider(IOptions<StripeOptions> stripeOptions, IOptions<MembershipOptions> membershipOptions)
    : IPaymentProvider, IDisposable
{
    private const string App = "mysync";
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private readonly SemaphoreSlim _setup = new(1, 1);
    private string? _priceId;
    private string? _portalConfigurationId;

    private StripeClient? _client;

    private StripeClient Client => _client ??= string.IsNullOrWhiteSpace(stripeOptions.Value.SecretKey)
        ? throw new PaymentProviderUnavailableException("Stripe:SecretKey is not configured.")
        : new StripeClient(stripeOptions.Value.SecretKey);

    /// <summary>UC17-R1: a new price when the configured one changes, so current subscribers keep theirs.</summary>
    private string LookupKey
    {
        get
        {
            var options = membershipOptions.Value;
            var cents = decimal.ToInt64(decimal.Round(options.Price * 100, MidpointRounding.AwayFromZero));
            return string.Create(CultureInfo.InvariantCulture, $"{App}-month-{cents}-{options.Currency.ToLowerInvariant()}");
        }
    }

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

    public Task<Uri> CreateCheckoutAsync(string customerId, OwnerId ownerId, Uri returnUrl, CancellationToken cancellationToken = default) =>
        CallAsync(async client =>
        {
            ArgumentNullException.ThrowIfNull(returnUrl);
            var priceId = await PriceIdAsync(client, cancellationToken).ConfigureAwait(false);
            var session = await new SessionService(client).CreateAsync(
                new SessionCreateOptions
                {
                    Mode = "subscription",
                    Customer = customerId,
                    ClientReferenceId = ownerId.Value,
                    LineItems = [new SessionLineItemOptions { Price = priceId, Quantity = 1 }],
                    SubscriptionData = new SessionSubscriptionDataOptions
                    {
                        Metadata = new Dictionary<string, string> { ["app"] = App, ["keycloak_id"] = ownerId.Value },
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
            var paymentMethod = subscription.DefaultPaymentMethod ?? await CustomerPaymentMethodAsync(client, customerId, cancellationToken).ConfigureAwait(false);
            var invoice = subscription.LatestInvoice;
            return new MembershipSnapshot(
                status,
                subscription.Id,
                subscription.Items.Data.FirstOrDefault()?.CurrentPeriodEnd is { } end ? ToUtc(end) : null,
                subscription.CancelAtPeriodEnd,
                paymentMethod?.Card is { } card ? new PaymentCard(card.Brand, card.Last4, (int)card.ExpMonth, (int)card.ExpYear) : null,
                await LastFailureAsync(client, customerId, cancellationToken).ConfigureAwait(false),
                status == MembershipStatus.PastDue && invoice is { Status: "open", HostedInvoiceUrl: { } url } ? new Uri(url) : null);
        });

    public void Dispose() => _setup.Dispose();

    /// <summary>UC17-R3.</summary>
    internal static MembershipStatus StatusOf(string status) => status switch
    {
        "active" or "trialing" => MembershipStatus.Active,
        "past_due" => MembershipStatus.PastDue,
        _ => MembershipStatus.Expired,
    };

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

    private async Task<string> PriceIdAsync(StripeClient client, CancellationToken cancellationToken)
    {
        if (_priceId is not null)
        {
            return _priceId;
        }

        await _setup.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_priceId is not null)
            {
                return _priceId;
            }

            var lookupKey = LookupKey;
            var prices = new PriceService(client);
            var existing = await prices.ListAsync(new PriceListOptions { LookupKeys = [lookupKey], Active = true, Limit = 1 }, cancellationToken: cancellationToken).ConfigureAwait(false);
            if (existing.Data.FirstOrDefault() is { } price)
            {
                return _priceId = price.Id;
            }

            var options = membershipOptions.Value;
            var created = await prices.CreateAsync(
                new PriceCreateOptions
                {
                    Currency = options.Currency.ToLowerInvariant(),
                    UnitAmountDecimal = options.Price * 100,
                    Recurring = new PriceRecurringOptions { Interval = "month" },
                    LookupKey = lookupKey,
                    TaxBehavior = "inclusive",
                    ProductData = new PriceProductDataOptions { Name = "MySync", Metadata = new Dictionary<string, string> { ["app"] = App } },
                    Metadata = new Dictionary<string, string> { ["app"] = App },
                },
                new RequestOptions { IdempotencyKey = $"{App}-price-{lookupKey}" },
                cancellationToken).ConfigureAwait(false);
            Logger.Info("Stripe price {LookupKey} created", lookupKey);
            return _priceId = created.Id;
        }
        finally
        {
            _setup.Release();
        }
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
