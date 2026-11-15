using FinHorizon.MySync.Application.Common;
using FinHorizon.MySync.Domain.Common;
using FinHorizon.MySync.Domain.Currencies;
using FinHorizon.MySync.Domain.Transactions;
using FinHorizon.MySync.Domain.Transactions.Kinds;
using FluentValidation;

namespace FinHorizon.MySync.Application.Features.Transactions;

/// <summary>The fields of a movement the user enters, shared by recording (UC5) and amending (UC8).</summary>
public interface ITransactionInput
{
    string? Kind { get; }

    DateOnly? Date { get; }

    decimal? Amount { get; }

    Guid? AccountId { get; }

    /// <summary>The destination account of a transfer (UC9).</summary>
    Guid? CounterAccountId { get; }

    /// <summary>What reaches the destination of a transfer between two currencies (UC13-R7).</summary>
    decimal? CounterAmount { get; }

    Guid? CategoryId { get; }

    Guid? DebtId { get; }

    string? Note { get; }
}

public static class TransactionInputExtensions
{
    /// <summary>For input already checked by <see cref="TransactionInputValidator{T}"/>.</summary>
    public static TransactionData ToData(this ITransactionInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        return new TransactionData(
            EnumNames.Parse<TransactionKind>(input.Kind),
            input.Date!.Value,
            new Money(input.Amount!.Value),
            input.AccountId!.Value,
            input.CategoryId,
            input.DebtId,
            input.Note,
            input.CounterAccountId,
            input.CounterAmount is { } counter ? new Money(counter) : null);
    }
}

/// <summary>UC5-R1, R2: the fields on their own; the rules on stored data are the domain's TransactionPolicy.</summary>
public abstract class TransactionInputValidator<T> : AbstractValidator<T>
    where T : ITransactionInput
{
    private static bool Uses(T input, Func<IMovementKind, bool> field) =>
        EnumNames.TryParse<TransactionKind>(input.Kind, out var kind) && field(MovementKinds.Of(kind));

    protected TransactionInputValidator()
    {
        RuleFor(input => input.Kind).RequiredEnumName<T, TransactionKind>();
        RuleFor(input => input.Date).Required();
        // The decimals of the account currency are checked by the domain rules (UC13-R2).
        RuleFor(input => input.Amount).RequiredPositiveAmount();
        RuleFor(input => input.CounterAmount)
            .Must(value => value is null || (value.Value > 0 && Money.HasValidPrecision(value.Value, CurrencyRules.MaxDecimals)))
            .WithErrorCode(ErrorCodes.InvalidValue);
        RuleFor(input => input.AccountId).Required();
        // Which fields a kind needs comes from the kind itself (IMovementKind).
        RuleFor(input => input.CategoryId).Required().When(input => Uses(input, kind => kind.UsesCategory));
        RuleFor(input => input.CounterAccountId).Required().When(input => Uses(input, kind => kind.UsesCounterAccount));
        RuleFor(input => input.Note).OptionalText(TransactionData.MaxNoteLength);
    }
}
