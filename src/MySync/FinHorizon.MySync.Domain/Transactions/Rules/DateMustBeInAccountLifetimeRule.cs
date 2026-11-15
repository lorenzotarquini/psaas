using FinHorizon.MySync.Domain.Common;

namespace FinHorizon.MySync.Domain.Transactions.Rules;

/// <summary>UC5-R5 (D3, D4), UC9-R4: from the opening of every account involved to today, both included.</summary>
public sealed class DateMustBeInAccountLifetimeRule : ITransactionRule
{
    public IEnumerable<FieldError> Check(TransactionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var date = context.Data.Date;
        if (date > context.Today
            || (context.Account is not null && date < context.Account.OpenedOn)
            || (context.CounterAccount is not null && date < context.CounterAccount.OpenedOn))
        {
            yield return new FieldError(TransactionFields.Date, ErrorCodes.InvalidValue);
        }
    }
}
