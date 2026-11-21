using FinHorizon.AdminPortal.Application.Contracts.Catalog;

namespace FinHorizon.AdminPortal.Application.Abstractions.Payments;

/// <summary>What Stripe needs of a plan (UC16-R5): the module it opens, the price and its VAT.</summary>
public sealed record PlanPublication(
    string Code,
    string Name,
    string? Description,
    string ModuleId,
    string ModuleName,
    decimal Price,
    string BillingPeriod,
    decimal VatRate,
    string? VatNature,
    bool PriceIncludesVat,
    int TrialDays,
    bool Active,
    int? Tier = null);

/// <summary>UC16 step 2: puts a plan on sale on Stripe (product, price, VAT rate) and keeps it up to date.</summary>
public interface IPlanPublisher
{
    /// <summary>False without a Stripe key: plans are saved but not published (UC16-2c).</summary>
    bool IsConfigured { get; }

    /// <summary>
    /// Creates or updates the plan on Stripe. A new price only when amount, period or VAT inclusion change (R3);
    /// the old one stays for the current subscribers. Throws <see cref="PaymentProviderUnavailableException"/>.
    /// </summary>
    Task<PlanStripeLink> PublishAsync(PlanPublication plan, PlanStripeLink? current, CancellationToken cancellationToken = default);
}

/// <summary>Stripe cannot be reached: nothing is saved, the request answers 503 (UC16-2b).</summary>
public sealed class PaymentProviderUnavailableException : Exception
{
    public PaymentProviderUnavailableException()
    {
    }

    public PaymentProviderUnavailableException(string message)
        : base(message)
    {
    }

    public PaymentProviderUnavailableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
