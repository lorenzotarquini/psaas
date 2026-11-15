using FinHorizon.MySync.Domain.Transactions;

namespace FinHorizon.MySync.Application.Features.Transactions;

/// <summary>
/// A movement with the names of what it points to. A transfer has no category and a destination account
/// (<see cref="CounterAccountId"/>); <see cref="AccountId"/> is its source. Amounts are in the account currencies (UC13):
/// <see cref="Amount"/> in <see cref="Currency"/>, what reaches the destination in <see cref="CounterCurrency"/>.
/// </summary>
public sealed record TransactionView(
    Guid Id,
    TransactionKind Kind,
    DateOnly Date,
    decimal Amount,
    Guid AccountId,
    string AccountName,
    Guid? CategoryId,
    string? CategoryName,
    Guid? DebtId,
    string? DebtName,
    string? Note,
    DateTimeOffset RecordedAt,
    Guid? CounterAccountId = null,
    string? CounterAccountName = null,
    string Currency = "EUR",
    decimal? CounterAmount = null,
    string? CounterCurrency = null);

/// <summary>Expenses and incomes of one day in one currency: the totals of a filter before conversion (UC13).</summary>
public sealed record DailyTotal(DateOnly Date, string Currency, decimal Expenses, decimal Incomes);

/// <summary>What the reader finds for a filter: one page and the per-day totals of the whole filter.</summary>
public sealed record TransactionSlice(IReadOnlyList<TransactionView> Items, int TotalCount, IReadOnlyList<DailyTotal> Totals);

/// <summary>What a list of movements is filtered by (UC8 step C3).</summary>
public sealed record TransactionFilter(
    DateOnly? From,
    DateOnly? To,
    TransactionKind? Kind,
    Guid? AccountId,
    Guid? CategoryId,
    Guid? DebtId,
    int Page,
    int PageSize);

/// <summary>One page of movements and the totals of the whole filter, in <see cref="Currency"/>, the display currency.</summary>
public sealed record TransactionPage(
    IReadOnlyList<TransactionView> Items,
    int Page,
    int PageSize,
    int TotalCount,
    decimal ExpenseTotal,
    decimal IncomeTotal,
    string Currency);
