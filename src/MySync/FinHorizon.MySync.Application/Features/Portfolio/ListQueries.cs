using FinHorizon.MySync.Application.Abstractions;
using FinHorizon.MySync.Application.Abstractions.Limits;
using FinHorizon.MySync.Application.Abstractions.Persistence;
using FinHorizon.MySync.Application.Common;
using FinHorizon.MySync.Application.Features.Accounts;
using FinHorizon.MySync.Application.Features.Debts;

namespace FinHorizon.MySync.Application.Features.Portfolio;

public sealed record ListAccountsQuery;

public sealed record ListDebtsQuery;

/// <summary>Tags already used on accounts and debts, suggested while typing (UC2-R3, UC3-R2).</summary>
public sealed record ListTagsQuery;

public sealed class ListAccountsHandler(ICurrentUser user, PortfolioReader portfolio, IResourceLimits limits)
    : IRequestHandler<ListAccountsQuery, AccountList>
{
    public async Task<Result<AccountList>> HandleAsync(ListAccountsQuery request, CancellationToken cancellationToken = default)
    {
        var current = await portfolio.ReadAsync(user.Id, cancellationToken).ConfigureAwait(false);
        var userLimits = await limits.ForAsync(user.Id, cancellationToken).ConfigureAwait(false);
        return Result<AccountList>.Ok(new AccountList(current.Accounts, userLimits.MaxAccounts));
    }
}

public sealed class ListDebtsHandler(ICurrentUser user, PortfolioReader portfolio, IResourceLimits limits)
    : IRequestHandler<ListDebtsQuery, DebtList>
{
    public async Task<Result<DebtList>> HandleAsync(ListDebtsQuery request, CancellationToken cancellationToken = default)
    {
        var current = await portfolio.ReadAsync(user.Id, cancellationToken).ConfigureAwait(false);
        var userLimits = await limits.ForAsync(user.Id, cancellationToken).ConfigureAwait(false);
        return Result<DebtList>.Ok(new DebtList(current.Debts, userLimits.MaxDebts));
    }
}

public sealed class ListTagsHandler(ICurrentUser user, IAccountRepository accounts, IDebtRepository debts)
    : IRequestHandler<ListTagsQuery, IReadOnlyList<string>>
{
    public async Task<Result<IReadOnlyList<string>>> HandleAsync(ListTagsQuery request, CancellationToken cancellationToken = default)
    {
        var accountTags = (await accounts.ListAsync(user.Id, cancellationToken).ConfigureAwait(false)).Select(account => account.Descriptor.Tag);
        var debtTags = (await debts.ListAsync(user.Id, cancellationToken).ConfigureAwait(false)).Select(debt => debt.Descriptor.Tag);
        IReadOnlyList<string> tags = accountTags
            .Concat(debtTags)
            .OfType<string>()
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .Order(StringComparer.CurrentCultureIgnoreCase)
            .ToList();
        return Result<IReadOnlyList<string>>.Ok(tags);
    }
}
