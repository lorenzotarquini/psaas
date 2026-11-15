using FinHorizon.MySync.Domain.Transactions;

namespace FinHorizon.MySync.Application.Features.Transactions;

/// <summary>A movement with the names of its account, category and debt.</summary>
public sealed record TransactionView(
    Guid Id,
    TransactionKind Kind,
    DateOnly Date,
    decimal Amount,
    Guid AccountId,
    string AccountName,
    Guid CategoryId,
    string CategoryName,
    Guid? DebtId,
    string? DebtName,
    string? Note,
    DateTimeOffset RecordedAt);

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

/// <summary>One page of movements and the totals of the whole filter.</summary>
public sealed record TransactionPage(
    IReadOnlyList<TransactionView> Items,
    int Page,
    int PageSize,
    int TotalCount,
    decimal ExpenseTotal,
    decimal IncomeTotal);
