using FinHorizon.MySync.Application.Abstractions;
using FinHorizon.MySync.Application.Abstractions.Persistence;
using FinHorizon.MySync.Application.Common;
using FinHorizon.MySync.Domain.Common;
using FinHorizon.MySync.Domain.Transactions;
using FinHorizon.MySync.Domain.Transactions.Kinds;
using FluentValidation;

namespace FinHorizon.MySync.Application.Features.Categories;

/// <summary>UC6 step 1: the user's categories of a kind containing the typed text.</summary>
public sealed record SearchCategoriesQuery(string? Kind, string? Term);

public sealed class SearchCategoriesValidator : AbstractValidator<SearchCategoriesQuery>
{
    public SearchCategoriesValidator() =>
        RuleFor(query => query.Kind)
            .Must(kind => kind is null || (EnumNames.TryParse<TransactionKind>(kind, out var parsed) && MovementKinds.WithCategory.Contains(parsed)))
            .WithErrorCode(ErrorCodes.InvalidValue);
}

public sealed class SearchCategoriesHandler(ICurrentUser user, ICategoryRepository categories)
    : IRequestHandler<SearchCategoriesQuery, IReadOnlyList<CategoryView>>
{
    /// <summary>Enough for a suggestion list; the user narrows by typing.</summary>
    public const int MaxResults = 50;

    public async Task<Result<IReadOnlyList<CategoryView>>> HandleAsync(SearchCategoriesQuery request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        TransactionKind? kind = request.Kind is null ? null : EnumNames.Parse<TransactionKind>(request.Kind);
        var found = await categories.SearchAsync(user.Id, kind, TextNormalizer.Clean(request.Term), MaxResults, cancellationToken).ConfigureAwait(false);
        return Result<IReadOnlyList<CategoryView>>.Ok(found.Select(CategoryView.From).ToList());
    }
}
