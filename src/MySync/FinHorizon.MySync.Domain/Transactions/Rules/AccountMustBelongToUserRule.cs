using FinHorizon.MySync.Domain.Common;

namespace FinHorizon.MySync.Domain.Transactions.Rules;

/// <summary>UC5-5b, UC9-R2: the accounts exist and are the user's; a transfer moves money between two different ones.</summary>
public sealed class AccountMustBelongToUserRule : ITransactionRule
{
    public IEnumerable<FieldError> Check(TransactionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (context.Account is null)
        {
            yield return new FieldError(TransactionFields.AccountId, ErrorCodes.InvalidValue);
        }

        if (context.Kind.UsesCounterAccount
            && (context.CounterAccount is null || context.Data.CounterAccountId == context.Data.AccountId))
        {
            yield return new FieldError(TransactionFields.CounterAccountId, ErrorCodes.InvalidValue);
        }
    }
}
