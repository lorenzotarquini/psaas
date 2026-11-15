using FinHorizon.MySync.Domain.Common;

namespace FinHorizon.MySync.Domain.Transactions.Rules;

/// <summary>UC5-5b, 5c, UC9-R3: the category is the user's and of the same kind as the movement; a transfer has none.</summary>
public sealed class CategoryMustMatchKindRule : ITransactionRule
{
    public IEnumerable<FieldError> Check(TransactionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var wrong = context.Kind.UsesCategory
            ? context.Category is null || context.Category.Kind != context.Data.Kind
            : context.Data.CategoryId is not null;
        if (wrong)
        {
            yield return new FieldError(TransactionFields.CategoryId, ErrorCodes.InvalidValue);
        }
    }
}
