namespace FinHorizon.MySync.Application.Common;

/// <summary>Enums travel by name ("CurrentAccount", "expense"), never by number.</summary>
public static class EnumNames
{
    public static bool TryParse<TEnum>(string? value, out TEnum result)
        where TEnum : struct, Enum
    {
        result = default;
        var trimmed = value?.Trim();
        return !string.IsNullOrEmpty(trimmed)
            && !char.IsDigit(trimmed[0])
            && trimmed[0] != '-'
            && Enum.TryParse(trimmed, ignoreCase: true, out result)
            && Enum.IsDefined(result);
    }

    /// <summary>For input already checked by a validator.</summary>
    public static TEnum Parse<TEnum>(string? value)
        where TEnum : struct, Enum =>
        TryParse<TEnum>(value, out var result) ? result : throw new ArgumentException($"'{value}' is not a {typeof(TEnum).Name}.", nameof(value));
}
