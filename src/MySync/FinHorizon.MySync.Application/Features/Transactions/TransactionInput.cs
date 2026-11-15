using FinHorizon.MySync.Application.Common;
using FinHorizon.MySync.Domain.Common;
using FinHorizon.MySync.Domain.Transactions;
using FluentValidation;

namespace FinHorizon.MySync.Application.Features.Transactions;

/// <summary>The fields of a movement the user enters, shared by recording (UC5) and amending (UC8).</summary>
public interface ITransactionInput
{
    string? Kind { get; }

    DateOnly? Date { get; }

    decimal? Amount { get; }

    Guid? AccountId { get; }

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
            input.CategoryId!.Value,
            input.DebtId,
            input.Note);
    }
}

/// <summary>UC5-R1, R2: the fields on their own; the rules on stored data are the domain's TransactionPolicy.</summary>
public abstract class TransactionInputValidator<T> : AbstractValidator<T>
    where T : ITransactionInput
{
    protected TransactionInputValidator()
    {
        RuleFor(input => input.Kind).RequiredEnumName<T, TransactionKind>();
        RuleFor(input => input.Date).Required();
        RuleFor(input => input.Amount).RequiredPositiveCents();
        RuleFor(input => input.AccountId).Required();
        RuleFor(input => input.CategoryId).Required();
        RuleFor(input => input.Note).OptionalText(TransactionData.MaxNoteLength);
    }
}
