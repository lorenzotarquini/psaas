using FinHorizon.MySync.Application.Abstractions;
using FinHorizon.MySync.Application.Abstractions.Persistence;
using FinHorizon.MySync.Application.Common;
using FinHorizon.MySync.Domain.Common;
using FinHorizon.MySync.Domain.Transactions;

namespace FinHorizon.MySync.Application.Features.Categories;

/// <summary>UC15: an expense category becomes fixed, or variable again.</summary>
public sealed record SetCategoryFixedCommand(Guid Id, bool Fixed);

/// <summary>UC15: only expenses are fixed (4a); a category of another user is not found (4b).</summary>
public sealed class SetCategoryFixedHandler(ICurrentUser user, ICategoryRepository categories, IUnitOfWork unitOfWork)
    : IRequestHandler<SetCategoryFixedCommand, CategoryView>
{
    public async Task<Result<CategoryView>> HandleAsync(SetCategoryFixedCommand request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var category = await categories.GetAsync(user.Id, request.Id, cancellationToken).ConfigureAwait(false);
        if (category is null)
        {
            return Result<CategoryView>.NotFound();
        }

        if (request.Fixed && category.Kind != TransactionKind.Expense)
        {
            return Result<CategoryView>.Invalid("fixed", ErrorCodes.InvalidValue);
        }

        category.SetFixed(request.Fixed);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result<CategoryView>.Ok(CategoryView.From(category));
    }
}
