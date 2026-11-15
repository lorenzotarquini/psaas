using FinHorizon.MySync.Application.Abstractions.Reading;
using FinHorizon.MySync.Application.Features.Transactions;
using FinHorizon.MySync.Domain.Common;
using FinHorizon.MySync.Domain.Transactions;
using FinHorizon.MySync.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;

namespace FinHorizon.MySync.Infrastructure.Reading;

/// <summary>
/// The movement list of UC4 and UC8 in SQL: joins for the names, the filter applied once in the WHERE,
/// the totals over the whole filter and not only the page.
/// </summary>
internal sealed class TransactionReader(MySyncDbContext db) : ITransactionReader
{
    private const string FromWhere = """
        FROM "Transactions" t
        JOIN "Accounts" a ON a."Id" = t."AccountId"
        LEFT JOIN "Categories" c ON c."Id" = t."CategoryId"
        LEFT JOIN "Accounts" ca ON ca."Id" = t."CounterAccountId"
        LEFT JOIN "Debts" d ON d."Id" = t."DebtId"
        WHERE t."OwnerId" = @owner
          AND (@id IS NULL OR t."Id" = @id)
          AND (@from IS NULL OR t."Date" >= @from)
          AND (@to IS NULL OR t."Date" <= @to)
          AND (@kind IS NULL OR t."Kind" = @kind)
          AND (@account IS NULL OR t."AccountId" = @account OR t."CounterAccountId" = @account)
          AND (@category IS NULL OR t."CategoryId" = @category)
          AND (@debt IS NULL OR t."DebtId" = @debt)
        """;

    private const string Columns = """
        SELECT t."Id", t."Kind", t."Date", t."Amount", t."AccountId", a."Name" AS "AccountName",
               t."CategoryId", c."Name" AS "CategoryName", t."DebtId", d."Name" AS "DebtName", t."Note", t."RecordedAt",
               t."CounterAccountId", ca."Name" AS "CounterAccountName",
               a."Currency", t."CounterAmount", ca."Currency" AS "CounterCurrency"
        """;

    public async Task<TransactionView?> GetAsync(OwnerId owner, Guid id, CancellationToken cancellationToken = default)
    {
        var filter = new TransactionFilter(null, null, null, null, null, null, 1, 1);
        var rows = await db.Database
            .SqlQueryRaw<Row>($"{Columns} {FromWhere}", Parameters(owner, filter, id))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return rows.Select(ToView).SingleOrDefault();
    }

    public async Task<TransactionSlice> ListAsync(OwnerId owner, TransactionFilter filter, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);
        var page = new NpgsqlParameter("limit", NpgsqlDbType.Integer) { Value = filter.PageSize };
        var offset = new NpgsqlParameter("offset", NpgsqlDbType.Integer) { Value = (filter.Page - 1) * filter.PageSize };
        var rows = await db.Database
            .SqlQueryRaw<Row>(
                $"""{Columns} {FromWhere} ORDER BY t."Date" DESC, t."RecordedAt" DESC LIMIT @limit OFFSET @offset""",
                [.. Parameters(owner, filter, null), page, offset])
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        // Per day and currency: the handler converts each with the rate of its day (UC13).
        var totals = await db.Database
            .SqlQueryRaw<Totals>(
                $"""
                 SELECT t."Date", a."Currency", COUNT(*)::int AS "Count",
                        COALESCE(SUM(t."Amount") FILTER (WHERE t."Kind" = 'Expense'), 0) AS "Expenses",
                        COALESCE(SUM(t."Amount") FILTER (WHERE t."Kind" = 'Income'), 0) AS "Incomes"
                 {FromWhere}
                 GROUP BY t."Date", a."Currency"
                 """,
                Parameters(owner, filter, null))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return new TransactionSlice(
            rows.Select(ToView).ToList(),
            totals.Sum(total => total.Count),
            totals.Select(total => new DailyTotal(total.Date, total.Currency, total.Expenses, total.Incomes)).ToList());
    }

    /// <summary>Fresh, typed parameters for each command: a null filter must still have a type for Postgres.</summary>
    private static object[] Parameters(OwnerId owner, TransactionFilter filter, Guid? id) =>
    [
        new NpgsqlParameter("owner", NpgsqlDbType.Varchar) { Value = owner.Value },
        Typed("id", NpgsqlDbType.Uuid, id),
        Typed("from", NpgsqlDbType.Date, filter.From),
        Typed("to", NpgsqlDbType.Date, filter.To),
        Typed("kind", NpgsqlDbType.Varchar, filter.Kind?.ToString()),
        Typed("account", NpgsqlDbType.Uuid, filter.AccountId),
        Typed("category", NpgsqlDbType.Uuid, filter.CategoryId),
        Typed("debt", NpgsqlDbType.Uuid, filter.DebtId)
    ];

    private static NpgsqlParameter Typed(string name, NpgsqlDbType type, object? value) =>
        new(name, type) { Value = value ?? DBNull.Value };

    private static TransactionView ToView(Row row) => new(
        row.Id,
        Enum.Parse<TransactionKind>(row.Kind),
        row.Date,
        new Money(row.Amount).Amount,
        row.AccountId,
        row.AccountName,
        row.CategoryId,
        row.CategoryName,
        row.DebtId,
        row.DebtName,
        row.Note,
        row.RecordedAt,
        row.CounterAccountId,
        row.CounterAccountName,
        row.Currency,
        row.CounterAmount is { } counter ? new Money(counter).Amount : null,
        row.CounterCurrency);

    private sealed class Row
    {
        public Guid Id { get; init; }

        public string Kind { get; init; } = string.Empty;

        public DateOnly Date { get; init; }

        public decimal Amount { get; init; }

        public Guid AccountId { get; init; }

        public string AccountName { get; init; } = string.Empty;

        public Guid? CategoryId { get; init; }

        public string? CategoryName { get; init; }

        public Guid? DebtId { get; init; }

        public string? DebtName { get; init; }

        public string? Note { get; init; }

        public DateTimeOffset RecordedAt { get; init; }

        public Guid? CounterAccountId { get; init; }

        public string? CounterAccountName { get; init; }

        public string Currency { get; init; } = string.Empty;

        public decimal? CounterAmount { get; init; }

        public string? CounterCurrency { get; init; }
    }

    private sealed class Totals
    {
        public DateOnly Date { get; init; }

        public string Currency { get; init; } = string.Empty;

        public int Count { get; init; }

        public decimal Expenses { get; init; }

        public decimal Incomes { get; init; }
    }
}
