using FinHorizon.MySync.Domain.Common;

namespace FinHorizon.MySync.Domain.Transactions.Rules;

/// <summary>
/// UC5-5d..5f, R4 and UC8-M3b, R3: only an expense can be an installment, of an existing open debt of the user,
/// dated from the debt creation, and not above the residual counted without this same movement.
/// </summary>
public sealed class InstallmentMustFitDebtRule : ITransactionRule
{
    public IEnumerable<FieldError> Check(TransactionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (context.Data.DebtId is null)
        {
            yield break;
        }

        var debt = context.Debt;
        var residual = context.DebtResidualWithoutThisTransaction;
        if (!context.Kind.CanPayDebt || debt is null || !residual.IsPositive)
        {
            yield return new FieldError(TransactionFields.DebtId, ErrorCodes.InvalidValue);
            yield break;
        }

        if (context.Data.Date < debt.CreatedOn)
        {
            yield return new FieldError(TransactionFields.Date, ErrorCodes.InvalidValue);
        }

        if (context.Data.Amount > residual)
        {
            yield return new FieldError(TransactionFields.Amount, ErrorCodes.InvalidValue);
        }
    }
}
