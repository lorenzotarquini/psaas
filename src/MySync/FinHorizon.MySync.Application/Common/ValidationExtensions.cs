using FinHorizon.MySync.Domain.Common;
using FluentValidation;

namespace FinHorizon.MySync.Application.Common;

/// <summary>The field rules every request shares, each with its error code (RNF5).</summary>
public static class ValidationExtensions
{
    public static IRuleBuilderOptions<T, string?> RequiredText<T>(this IRuleBuilder<T, string?> rule, int maxLength) =>
        rule
            .Must(value => !string.IsNullOrWhiteSpace(value)).WithErrorCode(ErrorCodes.Required)
            .Must(value => value is null || value.Trim().Length <= maxLength).WithErrorCode(ErrorCodes.MaxLengthExceeded);

    public static IRuleBuilderOptions<T, string?> OptionalText<T>(this IRuleBuilder<T, string?> rule, int maxLength) =>
        rule.Must(value => value is null || value.Trim().Length <= maxLength).WithErrorCode(ErrorCodes.MaxLengthExceeded);

    /// <summary>A required enum given by name, case-insensitive; numbers are not names.</summary>
    public static IRuleBuilderOptions<T, string?> RequiredEnumName<T, TEnum>(this IRuleBuilder<T, string?> rule)
        where TEnum : struct, Enum =>
        rule
            .Must(value => !string.IsNullOrWhiteSpace(value)).WithErrorCode(ErrorCodes.Required)
            .Must(value => value is null || EnumNames.TryParse<TEnum>(value, out _)).WithErrorCode(ErrorCodes.InvalidValue);

    public static IRuleBuilderOptions<T, decimal?> RequiredCents<T>(this IRuleBuilder<T, decimal?> rule) =>
        rule
            .NotNull().WithErrorCode(ErrorCodes.Required)
            .Must(value => value is null || Money.HasValidPrecision(value.Value)).WithErrorCode(ErrorCodes.InvalidFormat);

    public static IRuleBuilderOptions<T, decimal?> RequiredPositiveCents<T>(this IRuleBuilder<T, decimal?> rule) =>
        rule
            .RequiredCents()
            .Must(value => value is null || value.Value > 0).WithErrorCode(ErrorCodes.InvalidValue);

    public static IRuleBuilderOptions<T, TValue?> Required<T, TValue>(this IRuleBuilder<T, TValue?> rule)
        where TValue : struct =>
        rule.NotNull().WithErrorCode(ErrorCodes.Required);
}
