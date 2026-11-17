using FinHorizon.MySync.Domain.Common;

namespace FinHorizon.MySync.Domain.Transactions.Rules;

/// <summary>
/// UC16-R10, R11: only an expense that is not an installment pays a subscription of the user, from an account in the
/// subscription currency and dated between the first charge and the end.
/// </summary>
public sealed class PaymentMustFitSubscriptionRule : ITransactionRule
{
    public IEnumerable<FieldError> Check(TransactionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (context.Data.SubscriptionId is null)
        {
            yield break;
        }

        if (!context.Kind.CanPaySubscription || context.Data.DebtId is not null || context.Subscription is not { } subscription)
        {
            yield return new FieldError(TransactionFields.SubscriptionId, ErrorCodes.InvalidValue);
            yield break;
        }

        if (context.Account is { } account && account.Currency != subscription.Currency)
        {
            yield return new FieldError(TransactionFields.AccountId, ErrorCodes.InvalidValue);
        }

        if (context.Data.Date < subscription.FirstChargeOn || context.Data.Date > subscription.EndsOn)
        {
            yield return new FieldError(TransactionFields.Date, ErrorCodes.InvalidValue);
        }
    }
}
