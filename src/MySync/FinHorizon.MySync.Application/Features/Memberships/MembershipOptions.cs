namespace FinHorizon.MySync.Application.Features.Memberships;

/// <summary>UC17-R1 and R6: which module's plans MySync sells and where Stripe sends the user back.</summary>
public sealed class MembershipOptions
{
    public const string SectionName = "Membership";

    /// <summary>The module of the Admin Portal catalog whose plans (UC16) are on sale here.</summary>
    public string ModuleId { get; set; } = "mysync";

    /// <summary>The MySync frontend (e.g. http://localhost:5174): Stripe returns to its settings page.</summary>
    public Uri? FrontendUrl { get; set; }

    /// <summary>Where Stripe returns after a payment or the portal: always the billing tab of the MySync settings (R6).</summary>
    public Uri ReturnUrl(string query) =>
        new(FrontendUrl ?? throw new InvalidOperationException("Membership:FrontendUrl is not configured."), $"/settings?tab=billing&membership={query}");
}
