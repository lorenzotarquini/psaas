using FinHorizon.MySync.Domain.Common;

namespace FinHorizon.MySync.Domain.Transactions.Rules;

/// <summary>UC5-R5 (D3, D4): from the account opening to today, both included.</summary>
public sealed class DateMustBeInAccountLifetimeRule : ITransactionRule
{
    public IEnumerable<FieldError> Check(TransactionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var date = context.Data.Date;
        if (date > context.Today || (context.Account is not null && date < context.Account.OpenedOn))
        {
            yield return new FieldError(TransactionFields.Date, ErrorCodes.InvalidValue);
        }
    }
}
