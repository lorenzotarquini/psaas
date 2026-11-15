using FinHorizon.MySync.Application.Abstractions.Persistence;
using FinHorizon.MySync.Application.Abstractions.Reading;
using FinHorizon.MySync.Application.Features.Accounts;
using FinHorizon.MySync.Application.Features.Debts;
using FinHorizon.MySync.Domain.Common;

namespace FinHorizon.MySync.Application.Features.Portfolio;

/// <summary>Accounts with their balance and debts with their residual, as of today (UC4-R1, R2, R4).</summary>
public sealed record Portfolio(IReadOnlyList<AccountView> Accounts, IReadOnlyList<DebtView> Debts)
{
    /// <summary>The balances of all accounts; debts are shown next to it, not subtracted.</summary>
    public decimal NetWorth => Accounts.Sum(account => account.Balance);

    public decimal DebtsResidual => Debts.Sum(debt => debt.Residual);
}

/// <summary>Builds the <see cref="Portfolio"/> shared by the account and debt lists and the dashboard.</summary>
public sealed class PortfolioReader(IAccountRepository accounts, IDebtRepository debts, ILedgerReader ledger)
{
    public async Task<Portfolio> ReadAsync(OwnerId owner, CancellationToken cancellationToken = default)
    {
        var accountList = await accounts.ListAsync(owner, cancellationToken).ConfigureAwait(false);
        var debtList = await debts.ListAsync(owner, cancellationToken).ConfigureAwait(false);
        var movements = await ledger.MovementsByAccountAsync(owner, cancellationToken).ConfigureAwait(false);
        var installments = await ledger.InstallmentsByDebtAsync(owner, cancellationToken).ConfigureAwait(false);

        var accountViews = accountList
            .Select(account => AccountView.From(account, movements.GetValueOrDefault(account.Id)))
            .OrderBy(view => view.OpenedOn)
            .ThenBy(view => view.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
        var debtViews = debtList
            .Select(debt => DebtView.From(debt, installments.GetValueOrDefault(debt.Id)))
            .OrderBy(view => view.PaidOff)
            .ThenByDescending(view => view.Residual)
            .ThenBy(view => view.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
        return new Portfolio(accountViews, debtViews);
    }
}
