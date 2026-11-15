using FinHorizon.MySync.Domain.Common;

namespace FinHorizon.MySync.Domain.Transactions.Rules;

/// <summary>
/// One business rule of a movement (UC5, UC8). Rules are independent, so a phase of the product
/// adds or removes one without touching the others or the use cases.
/// </summary>
public interface ITransactionRule
{
    IEnumerable<FieldError> Check(TransactionContext context);
}
