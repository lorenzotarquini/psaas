using FinHorizon.MySync.Domain.Common;

namespace FinHorizon.MySync.Domain.Transactions.Rules;

/// <summary>
/// UC13-R2, R7, R8: each amount has the decimals of its account currency; a transfer between two currencies says what
/// reaches the destination, one in the same currency does not; an installment leaves from an account in the debt currency.
/// </summary>
public sealed class AmountsMustFitCurrenciesRule : ITransactionRule
{
    public IEnumerable<FieldError> Check(TransactionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var data = context.Data;
        if (context.Account is not { } account)
        {
            yield break;
        }

        if (!account.Fits(data.Amount))
        {
            yield return new FieldError(TransactionFields.Amount, ErrorCodes.InvalidFormat);
        }

        if (context.Debt is { } debt && debt.Currency != account.Currency)
        {
            yield return new FieldError(TransactionFields.AccountId, ErrorCodes.InvalidValue);
        }

        if (!context.Kind.UsesCounterAccount || context.CounterAccount is not { } destination)
        {
            yield break;
        }

        var sameCurrency = destination.Currency == account.Currency;
        if (sameCurrency && data.CounterAmount is not null)
        {
            yield return new FieldError(TransactionFields.CounterAmount, ErrorCodes.InvalidValue);
        }
        else if (!sameCurrency && data.CounterAmount is null)
        {
            yield return new FieldError(TransactionFields.CounterAmount, ErrorCodes.Required);
        }
        else if (!sameCurrency && data.CounterAmount is { } counter && !destination.Fits(counter))
        {
            yield return new FieldError(TransactionFields.CounterAmount, ErrorCodes.InvalidFormat);
        }
    }
}