using System.ComponentModel.DataAnnotations;

namespace FinHorizon.MySync.Application.Features.Memberships;

/// <summary>UC17-R1 and R6: price of MySync and where Stripe sends the user back.</summary>
public sealed class MembershipOptions
{
    public const string SectionName = "Membership";

    /// <summary>VAT included, per month.</summary>
    [Range(typeof(decimal), "0.50", "10000", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    public decimal Price { get; set; } = 4.99m;

    [RegularExpression("^[A-Z]{3}$")]
    public string Currency { get; set; } = "EUR";

    /// <summary>The MySync frontend (e.g. http://localhost:5174): Stripe returns to its settings page.</summary>
    public Uri? FrontendUrl { get; set; }

    /// <summary>Where Stripe returns after a payment or the portal: always the settings of MySync (R6).</summary>
    public Uri ReturnUrl(string query) =>
        new(FrontendUrl ?? throw new InvalidOperationException("Membership:FrontendUrl is not configured."), $"/settings?membership={query}");
}
