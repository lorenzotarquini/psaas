namespace FinHorizon.MySync.Domain.Common;

/// <summary>How MySync reads names the user types: case and outer spaces do not matter.</summary>
public static class TextNormalizer
{
    /// <summary>Trimmed text, or null when nothing is left.</summary>
    public static string? Clean(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }

    /// <summary>The key that makes a name unique per user.</summary>
    public static string Normalize(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value.Trim().ToUpperInvariant();
    }
}
