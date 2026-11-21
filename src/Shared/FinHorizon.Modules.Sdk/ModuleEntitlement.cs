namespace FinHorizon.Modules.Sdk;

/// <summary>Contract C3: where the subscription of a user to a module stands, as Stripe says.</summary>
public enum EntitlementStatus
{
    /// <summary>No paid subscription: read-only (C4).</summary>
    None = 0,

    /// <summary>Paid, or in trial.</summary>
    Active = 1,

    /// <summary>A renewal failed and Stripe is retrying: the user keeps the tier meanwhile.</summary>
    PastDue = 2,
}

/// <summary>
/// Contract C3–C6: what a user may do in a module. Built by the module from the tier of the paid plan and the tier
/// declared in its own manifest; the module decides everything from this one value.
/// </summary>
public sealed record ModuleEntitlement(EntitlementStatus Status, ModuleTier? Tier)
{
    /// <summary>C4: problem type of a change refused because nothing is paid (HTTP 402).</summary>
    public const string ExpiredProblemType = "membership-expired";

    /// <summary>C5: problem type of a feature outside the paid tier (HTTP 403), with the feature in "feature".</summary>
    public const string FeatureProblemType = "feature-not-in-tier";

    public static readonly ModuleEntitlement None = new(EntitlementStatus.None, null);

    /// <summary>C4: a paid tier is needed to change anything.</summary>
    public bool CanWrite => Status != EntitlementStatus.None && Tier is not null;

    /// <summary>C5: the paid tier includes the feature.</summary>
    public bool Has(string feature) => CanWrite && (Tier!.Features ?? []).Contains(feature);

    /// <summary>C6: the maximum of a limit in the paid tier; <paramref name="fallback"/> without a tier or without that limit.</summary>
    public int Limit(string key, int fallback) =>
        Tier?.Limits is { } limits && limits.TryGetValue(key, out var max) ? max : fallback;

    /// <summary>C3: the entitlement for the tier level a paid plan carries, among the tiers the module declares.</summary>
    public static ModuleEntitlement For(EntitlementStatus status, int? level, IReadOnlyList<ModuleTier> declared)
    {
        ArgumentNullException.ThrowIfNull(declared);
        if (status == EntitlementStatus.None || level is null) return None;
        var tier = declared.FirstOrDefault(t => t.Level == level);
        return tier is null ? None : new ModuleEntitlement(status, tier);
    }
}
