using System.Globalization;
using NLog;
using Stripe;
using FinHorizon.AdminPortal.Application.Abstractions.Payments;
using FinHorizon.AdminPortal.Application.Contracts.Catalog;

namespace FinHorizon.AdminPortal.Infrastructure.Payments;

/// <summary>
/// UC16 on Stripe: a plan is a product (metadata module and plan_code, so modules find their plans) whose default
/// price is the one on sale (lookup key plan-&lt;code&gt;); the VAT is a Stripe tax rate, created once per rate and reused.
/// </summary>
public sealed class StripePlanPublisher(string? secretKey) : IPlanPublisher
{
    private const string App = "finhorizon";
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private readonly StripeClient? _client = string.IsNullOrWhiteSpace(secretKey) ? null : new StripeClient(secretKey);

    public bool IsConfigured => _client is not null;

    public async Task<PlanStripeLink> PublishAsync(PlanPublication plan, PlanStripeLink? current, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(plan);
        var client = _client ?? throw new PaymentProviderUnavailableException("Stripe:SecretKey is not configured.");
        try
        {
            var taxRateId = await TaxRateAsync(client, plan, cancellationToken).ConfigureAwait(false);
            var metadata = new Dictionary<string, string>
            {
                ["app"] = App,
                ["module"] = plan.ModuleId,
                ["plan_code"] = plan.Code,
                ["tax_rate"] = taxRateId,
                ["trial_days"] = plan.TrialDays.ToString(CultureInfo.InvariantCulture),
            };
            var products = new ProductService(client);
            var productId = current?.ProductId;
            if (productId is null)
            {
                productId = (await products.CreateAsync(
                    new ProductCreateOptions { Name = plan.Name, Description = plan.Description, Active = plan.Active, Metadata = metadata },
                    new RequestOptions { IdempotencyKey = $"plan-product-{plan.Code}" },
                    cancellationToken).ConfigureAwait(false)).Id;
            }

            var priceId = await PriceAsync(client, productId, plan, current?.PriceId, cancellationToken).ConfigureAwait(false);
            // Name, description and sellability change the product only; the price on sale is its default one (R3, R5).
            await products.UpdateAsync(
                productId,
                new ProductUpdateOptions
                {
                    Name = plan.Name,
                    Description = plan.Description ?? string.Empty,
                    Active = plan.Active,
                    DefaultPrice = priceId,
                    Metadata = metadata,
                },
                cancellationToken: cancellationToken).ConfigureAwait(false);
            if (current?.PriceId is { } oldPrice && oldPrice != priceId)
            {
                // Not for sale any more; current subscribers keep paying it (R3).
                await new PriceService(client).UpdateAsync(oldPrice, new PriceUpdateOptions { Active = false }, cancellationToken: cancellationToken).ConfigureAwait(false);
            }

            Logger.Info("Plan {PlanCode} published on Stripe: product {ProductId}, price {PriceId}", plan.Code, productId, priceId);
            return new PlanStripeLink(productId, priceId, taxRateId);
        }
        catch (StripeException exception)
        {
            Logger.Error(exception, "Publishing plan {PlanCode} on Stripe failed: {StripeError}", plan.Code, exception.StripeError?.Code ?? exception.Message);
            throw new PaymentProviderUnavailableException("Stripe call failed.", exception);
        }
    }

    /// <summary>The current price if amount, period and VAT inclusion are the same; otherwise a new one (R3).</summary>
    private static async Task<string> PriceAsync(StripeClient client, string productId, PlanPublication plan, string? currentPriceId, CancellationToken cancellationToken)
    {
        var prices = new PriceService(client);
        var amount = decimal.ToInt64(plan.Price * 100);
        var interval = plan.BillingPeriod == BillingPeriods.Yearly ? "year" : "month";
        var taxBehavior = plan.PriceIncludesVat ? "inclusive" : "exclusive";
        if (currentPriceId is not null)
        {
            var currentPrice = await prices.GetAsync(currentPriceId, cancellationToken: cancellationToken).ConfigureAwait(false);
            if (currentPrice.UnitAmount == amount && currentPrice.Recurring?.Interval == interval && currentPrice.TaxBehavior == taxBehavior)
            {
                return currentPriceId;
            }
        }

        var price = await prices.CreateAsync(
            new PriceCreateOptions
            {
                Product = productId,
                Currency = "eur",
                UnitAmount = amount,
                Recurring = new PriceRecurringOptions { Interval = interval },
                TaxBehavior = taxBehavior,
                // The lookup key always names the price on sale: Stripe moves it from the old price to the new one.
                LookupKey = $"plan-{plan.Code}",
                TransferLookupKey = true,
                Metadata = new Dictionary<string, string> { ["app"] = App, ["plan_code"] = plan.Code },
            },
            new RequestOptions { IdempotencyKey = $"plan-price-{plan.Code}-{amount}-{interval}-{taxBehavior}" },
            cancellationToken).ConfigureAwait(false);
        return price.Id;
    }

    /// <summary>R4: one Italian tax rate per (rate, inclusion, nature), found again by its metadata.</summary>
    private static async Task<string> TaxRateAsync(StripeClient client, PlanPublication plan, CancellationToken cancellationToken)
    {
        var key = string.Create(CultureInfo.InvariantCulture, $"{plan.VatRate:0.##}|{(plan.PriceIncludesVat ? "inclusive" : "exclusive")}|{plan.VatNature ?? "-"}");
        var taxRates = new TaxRateService(client);
        await foreach (var taxRate in taxRates.ListAutoPagingAsync(new TaxRateListOptions { Active = true, Limit = 100 }, cancellationToken: cancellationToken).ConfigureAwait(false))
        {
            if (taxRate.Metadata.TryGetValue("vat_key", out var existing) && existing == key)
            {
                return taxRate.Id;
            }
        }

        var created = await taxRates.CreateAsync(
            new TaxRateCreateOptions
            {
                DisplayName = "IVA",
                Description = plan.VatRate > 0
                    ? string.Create(CultureInfo.InvariantCulture, $"IVA {plan.VatRate:0.##}%")
                    : $"IVA 0% - natura {plan.VatNature}",
                Percentage = plan.VatRate,
                Inclusive = plan.PriceIncludesVat,
                Country = "IT",
                Jurisdiction = "IT",
                TaxType = "vat",
                Metadata = new Dictionary<string, string> { ["app"] = App, ["vat_key"] = key, ["vat_nature"] = plan.VatNature ?? string.Empty },
            },
            new RequestOptions { IdempotencyKey = $"tax-rate-{key}" },
            cancellationToken).ConfigureAwait(false);
        Logger.Info("Stripe tax rate {VatKey} created", key);
        return created.Id;
    }
}
