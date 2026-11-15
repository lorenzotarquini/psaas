using FinHorizon.MySync.Domain.Common;

namespace FinHorizon.MySync.Domain.Transactions.Rules;

/// <summary>UC5-5b, 5c: the category is the user's and of the same kind as the movement.</summary>
public sealed class CategoryMustMatchKindRule : ITransactionRule
{
    public IEnumerable<FieldError> Check(TransactionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (context.Category is null || context.Category.Kind != context.Data.Kind)
        {
            yield return new FieldError(TransactionFields.CategoryId, ErrorCodes.InvalidValue);
        }
    }
}
