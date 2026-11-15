using FinHorizon.MySync.Application.Abstractions.Persistence;
using FinHorizon.MySync.Application.Abstractions.Reading;
using FinHorizon.MySync.Application.Features.Accounts;
using FinHorizon.MySync.Application.Features.Currencies;
using FinHorizon.MySync.Application.Features.Debts;
using FinHorizon.MySync.Domain.Common;
using FinHorizon.MySync.Domain.Currencies;
using FinHorizon.MySync.Domain.NetWorth;

namespace FinHorizon.MySync.Application.Features.Portfolio;

/// <summary>
/// Accounts with their balance and debts with their residual, as of today (UC4-R1, R2, R4), each also converted into
/// <see cref="Currency"/>, the display currency (UC13).
/// </summary>
public sealed record Portfolio(string Currency, IReadOnlyList<AccountView> Accounts, IReadOnlyList<DebtView> Debts)
{
    /// <summary>The balances of the accounts in the scope; debts are shown next to it, not subtracted (D9).</summary>
    public decimal NetWorthIn(PortfolioScope scope) =>
        Accounts.Where(account => scope.Includes(account.Kind)).Sum(account => account.DisplayBalance ?? 0m);

    public decimal DebtsResidualIn(PortfolioScope scope) =>
        Debts.Where(debt => scope.Includes(debt.Kind)).Sum(debt => debt.DisplayResidual ?? 0m);
}

/// <summary>Builds the <see cref="Portfolio"/> shared by the account and debt lists and the dashboard.</summary>
public sealed class PortfolioReader(
    IAccountRepository accounts,
    IDebtRepository debts,
    ILedgerReader ledger,
    CurrencyConverter converter,
    ICalendar calendar)
{
    /// <exception cref="Abstractions.Currencies.CurrencyServiceUnavailableException">A conversion is needed and the rates cannot be read (UC13-5a).</exception>
    public async Task<Portfolio> ReadAsync(OwnerId owner, CancellationToken cancellationToken = default)
    {
        var accountList = await accounts.ListAsync(owner, cancellationToken).ConfigureAwait(false);
        var debtList = await debts.ListAsync(owner, cancellationToken).ConfigureAwait(false);
        var movements = await ledger.MovementsByAccountAsync(owner, cancellationToken).ConfigureAwait(false);
        var installments = await ledger.InstallmentsByDebtAsync(owner, cancellationToken).ConfigureAwait(false);
        var display = await converter.DisplayCurrencyAsync(owner, cancellationToken).ConfigureAwait(false);
        var today = calendar.Today;
        RateTable rates = await converter
            .RatesAsync(display, accountList.Select(account => account.Currency).Concat(debtList.Select(debt => debt.Currency)), today, today, cancellationToken)
            .ConfigureAwait(false);

        var accountViews = accountList
            .Select(account => AccountView.From(account, movements.GetValueOrDefault(account.Id), rates, today))
            .OrderBy(view => view.OpenedOn)
            .ThenBy(view => view.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
        var debtViews = debtList
            .Select(debt => DebtView.From(debt, installments.GetValueOrDefault(debt.Id), rates, today))
            .OrderBy(view => view.PaidOff)
            .ThenByDescending(view => view.DisplayResidual)
            .ThenBy(view => view.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
        return new Portfolio(display, accountViews, debtViews);
    }
}
