using System.Text.RegularExpressions;

namespace FinHorizon.Modules.Sdk;

/// <summary>
/// Contract C1–C2 (docs/architettura-moduli.md): one subscription level of a module, as the module declares it.
/// Features are stable keys (e.g. "projection"); limits are maximum counts by key (e.g. "accounts": 10).
/// </summary>
public sealed record ModuleTier(int Level, string? Name, IReadOnlyList<string>? Features = null, IReadOnlyDictionary<string, int>? Limits = null);

/// <summary>Contract C1: the levels every module can sell, in order.</summary>
public static partial class TierLevels
{
    public const int Min = 1;

    public const int Max = 3;

    public static bool IsValid(int level) => level is >= Min and <= Max;

    /// <summary>
    /// Contract C1–C2: field path → error code ("InvalidValue", "Required", "InvalidFormat") for the declared tiers.
    /// Levels 1–3, each at most once; a name; feature and limit keys lowercase; limits ≥ 0; a higher level never
    /// gives less than a lower one (no feature lost, no limit lowered).
    /// </summary>
    public static IReadOnlyDictionary<string, string> Validate(IReadOnlyList<ModuleTier>? tiers)
    {
        var errors = new Dictionary<string, string>();
        if (tiers is null) return errors;
        var seen = new HashSet<int>();
        for (var i = 0; i < tiers.Count; i++)
        {
            var tier = tiers[i];
            if (!IsValid(tier.Level) || !seen.Add(tier.Level)) errors[$"tiers[{i}].level"] = "InvalidValue";
            if (string.IsNullOrWhiteSpace(tier.Name)) errors[$"tiers[{i}].name"] = "Required";
            if ((tier.Features ?? []).Any(f => !KeyRegex().IsMatch(f ?? string.Empty))) errors[$"tiers[{i}].features"] = "InvalidFormat";
            if ((tier.Limits ?? new Dictionary<string, int>()).Any(l => !KeyRegex().IsMatch(l.Key) || l.Value < 0)) errors[$"tiers[{i}].limits"] = "InvalidValue";
        }

        if (errors.Count > 0) return errors;
        var ordered = tiers.OrderBy(t => t.Level).ToArray();
        for (var i = 1; i < ordered.Length; i++)
        {
            var lower = ordered[i - 1];
            var higher = ordered[i];
            var index = tiers.ToList().IndexOf(higher);
            var losesFeature = (lower.Features ?? []).Except(higher.Features ?? []).Any();
            var lowersLimit = (lower.Limits ?? new Dictionary<string, int>())
                .Any(l => (higher.Limits ?? new Dictionary<string, int>()).TryGetValue(l.Key, out var max) ? max < l.Value : true);
            if (losesFeature || lowersLimit) errors[$"tiers[{index}]"] = "InvalidValue";
        }

        return errors;
    }

    [GeneratedRegex("^[a-z][a-z0-9-]{0,39}$")]
    private static partial Regex KeyRegex();
}
