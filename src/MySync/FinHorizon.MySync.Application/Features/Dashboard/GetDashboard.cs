using FinHorizon.MySync.Application.Abstractions;
using FinHorizon.MySync.Application.Abstractions.Limits;
using FinHorizon.MySync.Application.Abstractions.Reading;
using FinHorizon.MySync.Application.Common;
using FinHorizon.MySync.Application.Features.Accounts;
using FinHorizon.MySync.Application.Features.Debts;
using FinHorizon.MySync.Application.Features.Portfolio;
using FinHorizon.MySync.Application.Features.Transactions;

namespace FinHorizon.MySync.Application.Features.Dashboard;

public sealed record GetDashboardQuery;

/// <summary>UC4: today's net worth, accounts, debts, the latest movements and the limits for the "new" buttons.</summary>
public sealed record DashboardView(
    decimal NetWorth,
    decimal DebtsResidual,
    IReadOnlyList<AccountView> Accounts,
    IReadOnlyList<DebtView> Debts,
    IReadOnlyList<TransactionView> RecentTransactions,
    UserLimits Limits);

public sealed class GetDashboardHandler(ICurrentUser user, PortfolioReader portfolio, ITransactionReader transactions, IResourceLimits limits)
    : IRequestHandler<GetDashboardQuery, DashboardView>
{
    /// <summary>UC4 step 2.</summary>
    public const int RecentCount = 10;

    public async Task<Result<DashboardView>> HandleAsync(GetDashboardQuery request, CancellationToken cancellationToken = default)
    {
        var current = await portfolio.ReadAsync(user.Id, cancellationToken).ConfigureAwait(false);
        var recent = await transactions
            .ListAsync(user.Id, new TransactionFilter(null, null, null, null, null, null, 1, RecentCount), cancellationToken)
            .ConfigureAwait(false);
        var userLimits = await limits.ForAsync(user.Id, cancellationToken).ConfigureAwait(false);
        return Result<DashboardView>.Ok(new DashboardView(
            current.NetWorth,
            current.DebtsResidual,
            current.Accounts,
            current.Debts,
            recent.Items,
            userLimits));
    }
}
