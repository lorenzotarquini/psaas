using FinHorizon.MySync.Application.Abstractions.Persistence;
using FinHorizon.MySync.Application.Abstractions.Reading;
using FinHorizon.MySync.Domain.Common;
using FinHorizon.MySync.Domain.Transactions;
using FinHorizon.MySync.Domain.Transactions.Rules;

namespace FinHorizon.MySync.Application.Features.Transactions;

/// <summary>
/// Gathers the user's records a movement points to, so the <see cref="TransactionPolicy"/> can judge it.
/// Only records of the owner are found: another user's id behaves as a missing one (RNF2).
/// </summary>
public sealed class TransactionContextLoader(
    IAccountRepository accounts,
    ICategoryRepository categories,
    IDebtRepository debts,
    ILedgerReader ledger,
    ICalendar calendar)
{
    public async Task<TransactionContext> LoadAsync(
        OwnerId owner,
        TransactionData data,
        Guid? amendedTransactionId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(data);
        var account = await accounts.GetAsync(owner, data.AccountId, cancellationToken).ConfigureAwait(false);
        var counterAccount = data.CounterAccountId is { } counterId ? await accounts.GetAsync(owner, counterId, cancellationToken).ConfigureAwait(false) : null;
        var category = data.CategoryId is { } categoryId ? await categories.GetAsync(owner, categoryId, cancellationToken).ConfigureAwait(false) : null;
        var debt = data.DebtId is { } debtId ? await debts.GetAsync(owner, debtId, cancellationToken).ConfigureAwait(false) : null;
        var residual = debt is null
            ? Money.Zero
            : debt.ResidualAfter(await ledger.InstallmentsPaidAsync(owner, debt.Id, amendedTransactionId, cancellationToken).ConfigureAwait(false));
        return new TransactionContext(data, account, category, debt, residual, calendar.Today, counterAccount);
    }
}
