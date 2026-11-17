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

    /// <summary>Whether any movement touches the account: its currency is then fixed (UC13-R6).</summary>
    Task<bool> HasMovementsAsync(OwnerId owner, Guid accountId, CancellationToken cancellationToken = default);

    /// <summary>Installments paid per debt; debts without installments are missing.</summary>
    Task<IReadOnlyDictionary<Guid, Money>> InstallmentsByDebtAsync(OwnerId owner, CancellationToken cancellationToken = default);

    /// <summary>Installments paid on one debt, leaving out the movement being amended (UC8-M3b).</summary>
    Task<Money> InstallmentsPaidAsync(OwnerId owner, Guid debtId, Guid? exceptTransactionId, CancellationToken cancellationToken = default);

    /// <summary>UC16: the dates of the expenses linked to each subscription, or to one when <paramref name="subscriptionId"/> is given.</summary>
    Task<IReadOnlyList<SubscriptionPayment>> SubscriptionPaymentsAsync(OwnerId owner, Guid? subscriptionId = null, CancellationToken cancellationToken = default);

    /// <summary>The history of the accounts and debts in <paramref name="scope"/> only (UC12), in their currencies (UC13).</summary>
    Task<LedgerHistory> HistoryAsync(OwnerId owner, PortfolioScope scope, CancellationToken cancellationToken = default);
}

/// <summary>An expense linked to a subscription (UC16-R11): only its day matters.</summary>
public sealed record SubscriptionPayment(Guid SubscriptionId, DateOnly Date);
