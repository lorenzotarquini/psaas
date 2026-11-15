using FinHorizon.MySync.Domain.Common;

namespace FinHorizon.MySync.Domain.Transactions.Rules;

/// <summary>UC5-5b: the account exists and is the user's.</summary>
public sealed class AccountMustBelongToUserRule : ITransactionRule
{
    public IEnumerable<FieldError> Check(TransactionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (context.Account is null)
        {
            yield return new FieldError(TransactionFields.AccountId, ErrorCodes.InvalidValue);
        }
    }
}
