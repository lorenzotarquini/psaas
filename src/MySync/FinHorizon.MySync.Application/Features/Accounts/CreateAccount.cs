using FinHorizon.MySync.Application.Abstractions.Currencies;
using FinHorizon.MySync.Application.Common;
using FinHorizon.MySync.Domain.Accounts;
using FinHorizon.MySync.Domain.Common;
using FinHorizon.MySync.Domain.Currencies;
using FluentValidation;

namespace FinHorizon.MySync.Application.Features.Accounts;

/// <summary>The fields of an account the user enters, shared by creating (UC2) and editing (UC10).</summary>
public interface IAccountInput
{
    string? Kind { get; }

    string? Name { get; }

    string? Description { get; }

    string? Tag { get; }

    decimal? InitialBalance { get; }

    /// <summary>UC13: from the crypto catalog for "Moneta virtuale", required there; otherwise traditional, the display currency when missing.</summary>
    string? Currency { get; }
}

/// <summary>UC2: what the user enters for a new account.</summary>
public sealed record CreateAccountCommand(string? Kind, string? Name, string? Description, string? Tag, decimal? InitialBalance, string? Currency = null) : IAccountInput;

/// <summary>UC2-R1..R4, UC13-R2: the fields on their own and the currency against the catalog.</summary>
public abstract class AccountInputValidator<T> : AbstractValidator<T>
    where T : IAccountInput
{
    protected AccountInputValidator(ICurrencyCatalog catalog)
    {
        RuleFor(command => command.Kind).RequiredEnumName<T, AccountKind>();
        RuleFor(command => command.Name).RequiredText(Descriptor.MaxNameLength);
        RuleFor(command => command.Description).OptionalText(Descriptor.MaxDescriptionLength);
        RuleFor(command => command.Tag).OptionalText(Descriptor.MaxTagLength);
        RuleFor(command => command.InitialBalance)
            .RequiredAmount()
            .Must((command, value) => value is null || TypeOf(command) is not { } type || CurrencyRules.HasValidPrecision(value.Value, type))
            .WithErrorCode(ErrorCodes.InvalidFormat);
        RuleFor(command => command.Currency)
            .Must((command, code) => code is not null || TypeOf(command) != CurrencyType.Crypto).WithErrorCode(ErrorCodes.Required)
            .MustAsync(async (command, code, cancellationToken) =>
                code is null || TypeOf(command) is not { } type || await catalog.HasAsync(code, type, cancellationToken).ConfigureAwait(false))
            .WithErrorCode(ErrorCodes.InvalidValue);
    }

    /// <summary>The kind of currency the account kind takes, when the kind is valid.</summary>
    private static CurrencyType? TypeOf(T command) =>
        EnumNames.TryParse<AccountKind>(command.Kind, out var kind) ? CurrencyRules.TypeFor(kind) : null;
}

public sealed class CreateAccountValidator(ICurrencyCatalog catalog) : AccountInputValidator<CreateAccountCommand>(catalog);
