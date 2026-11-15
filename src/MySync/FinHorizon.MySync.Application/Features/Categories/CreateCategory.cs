using FinHorizon.MySync.Application.Abstractions;
using FinHorizon.MySync.Application.Abstractions.Persistence;
using FinHorizon.MySync.Application.Common;
using FinHorizon.MySync.Domain.Categories;
using FinHorizon.MySync.Domain.Common;
using FinHorizon.MySync.Domain.Transactions;
using FinHorizon.MySync.Domain.Transactions.Kinds;
using FluentValidation;

namespace FinHorizon.MySync.Application.Features.Categories;

/// <summary>UC6: a category typed in the movement form.</summary>
public sealed record CreateCategoryCommand(string? Kind, string? Name);

public sealed class CreateCategoryValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryValidator()
    {
        RuleFor(command => command.Kind)
            .RequiredEnumName<CreateCategoryCommand, TransactionKind>()
            .Must(kind => !EnumNames.TryParse<TransactionKind>(kind, out var parsed) || MovementKinds.WithCategory.Contains(parsed))
            .WithErrorCode(ErrorCodes.InvalidValue);
        RuleFor(command => command.Name).RequiredText(Category.MaxNameLength);
    }
}

/// <summary>
/// UC6-R4: idempotent. A category that already exists is returned as it is (Ok), a new one is Created,
/// also when another request created it a moment before.
/// </summary>
public sealed class CreateCategoryHandler(ICurrentUser user, ICategoryRepository categories, IUnitOfWork unitOfWork)
    : IRequestHandler<CreateCategoryCommand, CategoryView>
{
    public async Task<Result<CategoryView>> HandleAsync(CreateCategoryCommand request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var kind = EnumNames.Parse<TransactionKind>(request.Kind);
        var normalizedName = TextNormalizer.Normalize(request.Name!);

        var existing = await categories.FindByNameAsync(user.Id, kind, normalizedName, cancellationToken).ConfigureAwait(false);
        if (existing is not null)
        {
            return Result<CategoryView>.Ok(CategoryView.From(existing));
        }

        var category = Category.Create(user.Id, kind, request.Name!);
        categories.Add(category);
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DuplicateNameException)
        {
            var winner = await categories.FindByNameAsync(user.Id, kind, normalizedName, cancellationToken).ConfigureAwait(false);
            return winner is null ? throw new InvalidOperationException("A duplicate category vanished.") : Result<CategoryView>.Ok(CategoryView.From(winner));
        }

        return Result<CategoryView>.Created(CategoryView.From(category));
    }
}
