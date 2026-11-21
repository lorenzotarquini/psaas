using System.Globalization;
using NLog;
using Stripe;
using Stripe.Checkout;
using FinHorizon.Modules.Sdk;

namespace FinHorizon.Modules.Billing;

/// <summary>
/// Stripe for any module (docs/architettura-moduli.md, C3 and C7): one customer per user, the plans of the module
/// published by the Admin Portal (product metadata module, plan_code, tier, tax_rate, trial_days), Checkout, the
/// customer portal and the state of the subscription with its tier. The module keeps its own copy and its own rules.
/// </summary>
public sealed class StripeModuleBilling(ModuleBillingOptions options) : IDisposable
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private readonly SemaphoreSlim _setup = new(1, 1);
    private StripeClient? _client;
    private string? _portalConfigurationId;
    private (DateTimeOffset At, IReadOnlyList<BillingPlan> Plans)? _plans;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(options.SecretKey);

    private string ModuleId => string.IsNullOrWhiteSpace(options.ModuleId) ? throw new InvalidOperationException("ModuleBillingOptions.ModuleId is not set.") : options.ModuleId;

    private StripeClient Client => _client ??= IsConfigured
        ? new StripeClient(options.SecretKey)
        : throw new BillingUnavailableException("Stripe secret key is not configured.");

    /// <summary>
    /// The customer of the user: one per user on the whole platform (found again by metadata.keycloak_id),
    /// created when missing; asked twice at once, Stripe answers with the same one.
    /// </summary>
    public Task<string> CreateCustomerAsync(string userId, string? email, string? name, CancellationToken cancellationToken = default) =>
        CallAsync(async client =>
        {
            var customers = new CustomerService(client);
            var found = await customers.SearchAsync(
                new CustomerSearchOptions { Query = $"metadata['keycloak_id']:'{userId.Replace("'", string.Empty, StringComparison.Ordinal)}'", Limit = 1 },
                cancellationToken: cancellationToken).ConfigureAwait(false);
            if (found.Data.FirstOrDefault() is { } existing)
            {
                return existing.Id;
            }

            var customer = await customers.CreateAsync(
                new CustomerCreateOptions
                {
                    Email = email,
                    Name = name,
                    PreferredLocales = ["it", "en"],
                    Metadata = new Dictionary<string, string> { ["app"] = "finhorizon", ["keycloak_id"] = userId },
                },
                new RequestOptions { IdempotencyKey = $"customer-{userId}" },
                cancellationToken).ConfigureAwait(false);
            Logger.Info("Stripe customer created for {UserId}", userId);
            return customer.Id;
        });

    /// <summary>The plans of the module on sale, monthly first, cheapest first (cached for a minute).</summary>
    public Task<IReadOnlyList<BillingPlan>> GetPlansAsync(CancellationToken cancellationToken = default) =>
        CallAsync<IReadOnlyList<BillingPlan>>(async client =>
        {
            if (_plans is { } cached && cached.At > DateTimeOffset.UtcNow.AddMinutes(-1))
            {
                return cached.Plans;
            }

            var plans = new List<BillingPlan>();
            var products = new ProductService(client).ListAutoPagingAsync(
                new ProductListOptions { Active = true, Limit = 100, Expand = ["data.default_price"] },
                cancellationToken: cancellationToken);
            await foreach (var product in products.ConfigureAwait(false))
            {
                if (!product.Metadata.TryGetValue("module", out var module) || module != ModuleId
                    || !product.Metadata.TryGetValue("plan_code", out var code)
                    || product.DefaultPrice is not { Active: true, UnitAmount: { } amount, Recurring: { } recurring } price)
                {
                    continue;
                }

                product.Metadata.TryGetValue("tax_rate", out var taxRateId);
                product.Metadata.TryGetValue("trial_days", out var trialDays);
                plans.Add(new BillingPlan(
                    code,
                    product.Name,
                    string.IsNullOrWhiteSpace(product.Description) ? null : product.Description,
                    TierOf(product),
                    amount / 100m,
                    price.Currency.ToUpperInvariant(),
                    recurring.Interval == "year" ? "Year" : "Month",
                    price.TaxBehavior != "exclusive",
                    await VatRateAsync(client, taxRateId, cancellationToken).ConfigureAwait(false),
                    int.TryParse(trialDays, NumberStyles.Integer, CultureInfo.InvariantCulture, out var days) ? days : 0,
                    price.Id,
                    string.IsNullOrWhiteSpace(taxRateId) ? null : taxRateId));
            }

            var sorted = plans.OrderBy(plan => plan.Tier ?? 0).ThenBy(plan => plan.Interval == "Year" ? 1 : 0).ThenBy(plan => plan.Price).ToList();
            _plans = (DateTimeOffset.UtcNow, sorted);
            return sorted;
        });

    /// <summary>Stripe's payment page for the plan, with its VAT on the subscription item and, when asked, its trial.</summary>
    public Task<Uri> CreateCheckoutAsync(string customerId, string userId, BillingPlan plan, bool withTrial, Uri returnUrl, CancellationToken cancellationToken = default) =>
        CallAsync(async client =>
        {
            ArgumentNullException.ThrowIfNull(plan);
            ArgumentNullException.ThrowIfNull(returnUrl);
            var session = await new SessionService(client).CreateAsync(
                new SessionCreateOptions
                {
                    Mode = "subscription",
                    Customer = customerId,
                    ClientReferenceId = userId,
                    LineItems = [new SessionLineItemOptions { Price = plan.PriceId, Quantity = 1, TaxRates = plan.TaxRateId is null ? null : [plan.TaxRateId] }],
                    SubscriptionData = new SessionSubscriptionDataOptions
                    {
                        TrialPeriodDays = withTrial && plan.TrialDays > 0 ? plan.TrialDays : null,
                        Metadata = new Dictionary<string, string> { ["module"] = ModuleId, ["keycloak_id"] = userId, ["plan_code"] = plan.Code },
                    },
                    SuccessUrl = returnUrl.ToString(),
                    CancelUrl = returnUrl.ToString(),
                    Locale = "auto",
                },
                cancellationToken: cancellationToken).ConfigureAwait(false);
            return new Uri(session.Url);
        });

    /// <summary>
    /// Moves a running subscription to another plan of the module (an upgrade): the difference for the rest of the period
    /// is invoiced and charged at once, and the new plan applies only if that payment succeeds (pending_if_incomplete);
    /// otherwise the subscription stays as it was and the failure is returned. A different VAT rate applies from the next
    /// invoice (Stripe does not take tax rates in an update that waits for payment).
    /// </summary>
    public Task<BillingPlanChange> ChangePlanAsync(string customerId, string subscriptionId, BillingPlan plan, CancellationToken cancellationToken = default) =>
        CallAsync(async client =>
        {
            ArgumentNullException.ThrowIfNull(plan);
            var subscriptions = new SubscriptionService(client);
            var subscription = await subscriptions.GetAsync(subscriptionId, cancellationToken: cancellationToken).ConfigureAwait(false);
            var item = subscription.Items.Data.FirstOrDefault() ?? throw new BillingUnavailableException($"Subscription {subscriptionId} has no item.");
            var updated = await subscriptions.UpdateAsync(
                subscriptionId,
                new SubscriptionUpdateOptions
                {
                    // pending_if_incomplete accepts the price only: the item keeps its VAT, aligned below once paid.
                    Items = [new SubscriptionItemOptions { Id = item.Id, Price = plan.PriceId }],
                    ProrationBehavior = "always_invoice",
                    PaymentBehavior = "pending_if_incomplete",
                },
                cancellationToken: cancellationToken).ConfigureAwait(false);
            if (updated.PendingUpdate is not null)
            {
                Logger.Info("Plan change of subscription {SubscriptionId} to {PlanCode} waits for a payment that did not go through", subscriptionId, plan.Code);
                return new BillingPlanChange(false, await LastFailureAsync(client, customerId, cancellationToken).ConfigureAwait(false));
            }

            // The VAT of the new plan from the next invoice on (the difference just paid used the item's), and the
            // plan code, informational only (the tier is read from the product).
            var taxRates = item.TaxRates?.Select(rate => rate.Id).ToList() ?? [];
            var sameVat = plan.TaxRateId is null || (taxRates.Count == 1 && taxRates[0] == plan.TaxRateId);
            await subscriptions.UpdateAsync(
                subscriptionId,
                new SubscriptionUpdateOptions
                {
                    Items = sameVat ? null : [new SubscriptionItemOptions { Id = item.Id, TaxRates = [plan.TaxRateId!] }],
                    ProrationBehavior = sameVat ? null : "none",
                    Metadata = new Dictionary<string, string> { ["plan_code"] = plan.Code },
                },
                cancellationToken: cancellationToken).ConfigureAwait(false);
            Logger.Info("Subscription {SubscriptionId} moved to plan {PlanCode}", subscriptionId, plan.Code);
            return new BillingPlanChange(true, null);
        });

    /// <summary>Stripe's customer portal: payment method, invoices, cancellation at the end of the period.</summary>
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

    /// <summary>Contract C3: the subscription of the customer to this module now, the running one before the ended ones.</summary>
    public Task<BillingSubscription> GetSubscriptionAsync(string customerId, CancellationToken cancellationToken = default) =>
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
            var subscription = subscriptions.Data
                .Where(item => (item.Metadata.TryGetValue("module", out var module) || item.Metadata.TryGetValue("app", out module)) && module == ModuleId)
                .OrderBy(item => StatusOf(item.Status) == EntitlementStatus.None ? 1 : 0)
                .ThenByDescending(item => item.Created)
                .FirstOrDefault();
            if (subscription is null)
            {
                return BillingSubscription.None;
            }

            var status = StatusOf(subscription.Status);
            var paymentMethod = subscription.DefaultPaymentMethod ?? await CustomerPaymentMethodAsync(client, customerId, cancellationToken).ConfigureAwait(false);
            var invoice = subscription.LatestInvoice;
            return new BillingSubscription(
                status,
                subscription.Id,
                await SubscribedPlanAsync(client, subscription, cancellationToken).ConfigureAwait(false),
                subscription.Items.Data.FirstOrDefault()?.CurrentPeriodEnd is { } end ? ToUtc(end) : null,
                subscription.CancelAtPeriodEnd,
                paymentMethod?.Card is { } card ? new BillingCard(card.Brand, card.Last4, (int)card.ExpMonth, (int)card.ExpYear) : null,
                await LastFailureAsync(client, customerId, cancellationToken).ConfigureAwait(false),
                status == EntitlementStatus.PastDue && invoice is { Status: "open", HostedInvoiceUrl: { } url } ? new Uri(url) : null);
        });

    public void Dispose() => _setup.Dispose();

    /// <summary>Contract C3: paid (or in trial) and, while Stripe retries, past due; anything else is no tier.</summary>
    public static EntitlementStatus StatusOf(string status) => status switch
    {
        "active" or "trialing" => EntitlementStatus.Active,
        "past_due" => EntitlementStatus.PastDue,
        _ => EntitlementStatus.None,
    };

    private static int? TierOf(Product product) =>
        product.Metadata.TryGetValue("tier", out var tier) && int.TryParse(tier, NumberStyles.Integer, CultureInfo.InvariantCulture, out var level) ? level : null;

    private static DateTimeOffset ToUtc(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));

    private static async Task<BillingSubscribedPlan?> SubscribedPlanAsync(StripeClient client, Subscription subscription, CancellationToken cancellationToken)
    {
        if (subscription.Items.Data.FirstOrDefault()?.Price is not { UnitAmount: { } amount } price)
        {
            return null;
        }

        var product = await new ProductService(client).GetAsync(price.ProductId, cancellationToken: cancellationToken).ConfigureAwait(false);
        return new BillingSubscribedPlan(product.Name, TierOf(product), amount / 100m, price.Currency.ToUpperInvariant(), price.Recurring?.Interval == "year" ? "Year" : "Month");
    }

    private static async Task<decimal?> VatRateAsync(StripeClient client, string? taxRateId, CancellationToken cancellationToken) =>
        string.IsNullOrWhiteSpace(taxRateId)
            ? null
            : (await new TaxRateService(client).GetAsync(taxRateId, cancellationToken: cancellationToken).ConfigureAwait(false)).Percentage;

    private static async Task<PaymentMethod?> CustomerPaymentMethodAsync(StripeClient client, string customerId, CancellationToken cancellationToken)
    {
        var customer = await new CustomerService(client).GetAsync(
            customerId,
            new CustomerGetOptions { Expand = ["invoice_settings.default_payment_method"] },
            cancellationToken: cancellationToken).ConfigureAwait(false);
        return customer.InvoiceSettings?.DefaultPaymentMethod;
    }

    private static async Task<BillingFailure?> LastFailureAsync(StripeClient client, string customerId, CancellationToken cancellationToken)
    {
        var intents = await new PaymentIntentService(client).ListAsync(
            new PaymentIntentListOptions { Customer = customerId, Limit = 1 },
            cancellationToken: cancellationToken).ConfigureAwait(false);
        return intents.Data.FirstOrDefault() is { Status: not "succeeded", LastPaymentError: { } error } intent
            ? new BillingFailure(error.DeclineCode ?? error.Code, error.Message, ToUtc(intent.Created))
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
            if (existing.Data.FirstOrDefault(item => item.Metadata.TryGetValue("app", out var app) && app == ModuleId) is { } configuration)
            {
                return _portalConfigurationId = configuration.Id;
            }

            var created = await configurations.CreateAsync(
                new Stripe.BillingPortal.ConfigurationCreateOptions
                {
                    BusinessProfile = new Stripe.BillingPortal.ConfigurationBusinessProfileOptions { Headline = "FinHorizon" },
                    Features = new Stripe.BillingPortal.ConfigurationFeaturesOptions
                    {
                        PaymentMethodUpdate = new Stripe.BillingPortal.ConfigurationFeaturesPaymentMethodUpdateOptions { Enabled = true },
                        InvoiceHistory = new Stripe.BillingPortal.ConfigurationFeaturesInvoiceHistoryOptions { Enabled = true },
                        SubscriptionCancel = new Stripe.BillingPortal.ConfigurationFeaturesSubscriptionCancelOptions { Enabled = true, Mode = "at_period_end" },
                    },
                    Metadata = new Dictionary<string, string> { ["app"] = ModuleId },
                },
                cancellationToken: cancellationToken).ConfigureAwait(false);
            Logger.Info("Stripe customer portal configuration created for module {ModuleId}", ModuleId);
            return _portalConfigurationId = created.Id;
        }
        finally
        {
            _setup.Release();
        }
    }

    /// <summary>Stripe errors become <see cref="BillingUnavailableException"/> (503 for the user); the cause stays in the log.</summary>
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
            throw new BillingUnavailableException("Stripe call failed.", exception);
        }
    }
}
