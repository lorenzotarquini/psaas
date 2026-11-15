using FinHorizon.MySync.Domain.Categories;
using FinHorizon.MySync.Domain.Transactions;

namespace FinHorizon.MySync.Application.Features.Categories;

public sealed record CategoryView(Guid Id, TransactionKind Kind, string Name)
{
    public static CategoryView From(Category category)
    {
        ArgumentNullException.ThrowIfNull(category);
        return new CategoryView(category.Id, category.Kind, category.Name);
    }
}
