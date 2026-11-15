using FinHorizon.MySync.Application.Abstractions;
using FinHorizon.MySync.Application.Abstractions.Reading;
using FinHorizon.MySync.Application.Common;
using FinHorizon.MySync.Domain.Common;
using FinHorizon.MySync.Domain.Transactions;
using FluentValidation;

namespace FinHorizon.MySync.Application.Features.Transactions;

/// <summary>UC8 "Consultare" and the recent movements of UC4.</summary>
public sealed record ListTransactionsQuery(
    DateOnly? From = null,
    DateOnly? To = null,
    string? Kind = null,
    Guid? AccountId = null,
    Guid? CategoryId = null,
    Guid? DebtId = null,
    int? Page = null,
    int? PageSize = null)
{
    public const int DefaultPageSize = 25;
    public const int MaxPageSize = 100;
}

public sealed class ListTransactionsValidator : AbstractValidator<ListTransactionsQuery>
{
    public ListTransactionsValidator()
    {
        // UC8-C3b.
        RuleFor(query => query)
            .Must(query => query.From is null || query.To is null || query.From <= query.To)
            .WithErrorCode(ErrorCodes.InvalidValue)
            .OverridePropertyName("period");
        RuleFor(query => query.Kind)
            .Must(kind => kind is null || EnumNames.TryParse<TransactionKind>(kind, out _))
            .WithErrorCode(ErrorCodes.InvalidValue);
        RuleFor(query => query.Page).GreaterThanOrEqualTo(1).WithErrorCode(ErrorCodes.InvalidValue);
        RuleFor(query => query.PageSize).InclusiveBetween(1, ListTransactionsQuery.MaxPageSize).WithErrorCode(ErrorCodes.InvalidValue);
    }
}

public sealed class ListTransactionsHandler(ICurrentUser user, ITransactionReader reader)
    : IRequestHandler<ListTransactionsQuery, TransactionPage>
{
    public async Task<Result<TransactionPage>> HandleAsync(ListTransactionsQuery request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var filter = new TransactionFilter(
            request.From,
            request.To,
            request.Kind is null ? null : EnumNames.Parse<TransactionKind>(request.Kind),
            request.AccountId,
            request.CategoryId,
            request.DebtId,
            request.Page ?? 1,
            request.PageSize ?? ListTransactionsQuery.DefaultPageSize);
        return Result<TransactionPage>.Ok(await reader.ListAsync(user.Id, filter, cancellationToken).ConfigureAwait(false));
    }
}
