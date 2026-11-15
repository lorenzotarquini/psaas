using FinHorizon.MySync.Domain.Common;

namespace FinHorizon.MySync.Domain.NetWorth;

/// <summary>
/// Net worth (the balances of all accounts) and the debts still to pay at the end of one day (UC7).
/// Debts are shown next to the net worth, not subtracted from it.
/// </summary>
public sealed record NetWorthPoint(DateOnly Date, Money NetWorth, Money Debts);
