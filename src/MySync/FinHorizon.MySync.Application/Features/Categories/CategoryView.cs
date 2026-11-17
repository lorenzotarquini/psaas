using FinHorizon.MySync.Domain.Categories;
using FinHorizon.MySync.Domain.Transactions;

namespace FinHorizon.MySync.Application.Features.Categories;

/// <summary><see cref="Fixed"/> is always false for income categories (UC15).</summary>
public sealed record CategoryView(Guid Id, TransactionKind Kind, string Name, bool Fixed)
{
    public static CategoryView From(Category category)
    {
        ArgumentNullException.ThrowIfNull(category);
        return new CategoryView(category.Id, category.Kind, category.Name, category.Fixed);
    }
}
