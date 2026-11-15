using FinHorizon.MySync.Domain.Common;
using FinHorizon.MySync.Domain.NetWorth;

namespace FinHorizon.MySync.Application.Abstractions.Reading;

/// <summary>
/// Sums over the movements of a user. Balances and residuals are always computed, never stored, so they cannot drift (UC4-R5).
/// </summary>
public interface ILedgerReader
{
    /// <summary>Incomes minus expenses per account; accounts without movements are missing.</summary>
    Task<IReadOnlyDictionary<Guid, Money>> MovementsByAccountAsync(OwnerId owner, CancellationToken cancellationToken = default);

    /// <summary>Installments paid per debt; debts without installments are missing.</summary>
    Task<IReadOnlyDictionary<Guid, Money>> InstallmentsByDebtAsync(OwnerId owner, CancellationToken cancellationToken = default);

    /// <summary>Installments paid on one debt, leaving out the movement being amended (UC8-M3b).</summary>
    Task<Money> InstallmentsPaidAsync(OwnerId owner, Guid debtId, Guid? exceptTransactionId, CancellationToken cancellationToken = default);

    Task<LedgerHistory> HistoryAsync(OwnerId owner, CancellationToken cancellationToken = default);
}
