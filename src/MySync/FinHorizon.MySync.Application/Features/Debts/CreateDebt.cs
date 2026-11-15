using FinHorizon.MySync.Application.Abstractions.Currencies;
using FinHorizon.MySync.Application.Common;
using FinHorizon.MySync.Domain.Common;
using FinHorizon.MySync.Domain.Currencies;
using FinHorizon.MySync.Domain.Debts;
using FluentValidation;

namespace FinHorizon.MySync.Application.Features.Debts;

/// <summary>The fields of a debt the user enters, shared by creating (UC3) and editing (UC10).</summary>
public interface IDebtInput
{
    string? Kind { get; }

    string? Name { get; }

    string? Description { get; }

    string? Tag { get; }

    Guid? AccountId { get; }

    /// <summary>The initial residual: on creation, what is left to pay today.</summary>
    decimal? Residual { get; }

    /// <summary>UC13, D12: a traditional currency, the display currency when missing.</summary>
    string? Currency { get; }
}

/// <summary>UC3: what the user enters for a new debt.</summary>
public sealed record CreateDebtCommand(string? Kind, string? Name, string? Description, string? Tag, Guid? AccountId, decimal? Residual, string? Currency = null) : IDebtInput;

/// <summary>UC3-R1..R3: the fields on their own.</summary>
public abstract class DebtInputValidator<T> : AbstractValidator<T>
    where T : IDebtInput
{
    protected DebtInputValidator(ICurrencyCatalog catalog)
    {
        RuleFor(command => command.Currency)
            .MustAsync(async (code, cancellationToken) => code is null || await catalog.HasAsync(code, CurrencyType.Fiat, cancellationToken).ConfigureAwait(false))
            .WithErrorCode(ErrorCodes.InvalidValue);
        RuleFor(command => command.Kind).RequiredEnumName<T, DebtKind>();
        RuleFor(command => command.Name).RequiredText(Descriptor.MaxNameLength);
        RuleFor(command => command.Description).OptionalText(Descriptor.MaxDescriptionLength);
        RuleFor(command => command.Tag).OptionalText(Descriptor.MaxTagLength);
        RuleFor(command => command.AccountId).Required();
        RuleFor(command => command.Residual).RequiredPositiveCents();
    }
}

public sealed class CreateDebtValidator(ICurrencyCatalog catalog) : DebtInputValidator<CreateDebtCommand>(catalog);
