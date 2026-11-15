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
        JOIN "Categories" c ON c."Id" = t."CategoryId"
        LEFT JOIN "Debts" d ON d."Id" = t."DebtId"
        WHERE t."OwnerId" = @owner
          AND (@id IS NULL OR t."Id" = @id)
          AND (@from IS NULL OR t."Date" >= @from)
          AND (@to IS NULL OR t."Date" <= @to)
          AND (@kind IS NULL OR t."Kind" = @kind)
          AND (@account IS NULL OR t."AccountId" = @account)
          AND (@category IS NULL OR t."CategoryId" = @category)
          AND (@debt IS NULL OR t."DebtId" = @debt)
        """;

    private const string Columns = """
        SELECT t."Id", t."Kind", t."Date", t."Amount", t."AccountId", a."Name" AS "AccountName",
               t."CategoryId", c."Name" AS "CategoryName", t."DebtId", d."Name" AS "DebtName", t."Note", t."RecordedAt"
        """;

    public async Task<TransactionView?> GetAsync(OwnerId owner, Guid id, CancellationToken cancellationToken = default)
    {
        var filter = new TransactionFilter(null, null, null, null, null, null, 1, 1);
        var rows = await db.Database
            .SqlQueryRaw<Row>($"{Columns} {FromWhere}", Parameters(owner, filter, id))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return rows.Select(ToView).SingleOrDefault();
    }

    public async Task<TransactionPage> ListAsync(OwnerId owner, TransactionFilter filter, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);
        var page = new NpgsqlParameter("limit", NpgsqlDbType.Integer) { Value = filter.PageSize };
        var offset = new NpgsqlParameter("offset", NpgsqlDbType.Integer) { Value = (filter.Page - 1) * filter.PageSize };
        var rows = await db.Database
            .SqlQueryRaw<Row>(
                $"""{Columns} {FromWhere} ORDER BY t."Date" DESC, t."RecordedAt" DESC LIMIT @limit OFFSET @offset""",
                [.. Parameters(owner, filter, null), page, offset])
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        var totals = await db.Database
            .SqlQueryRaw<Totals>(
                $"""
                 SELECT COUNT(*)::int AS "Count",
                        COALESCE(SUM(t."Amount") FILTER (WHERE t."Kind" = 'Expense'), 0) AS "Expenses",
                        COALESCE(SUM(t."Amount") FILTER (WHERE t."Kind" = 'Income'), 0) AS "Incomes"
                 {FromWhere}
                 """,
                Parameters(owner, filter, null))
            .SingleAsync(cancellationToken).ConfigureAwait(false);
        return new TransactionPage(rows.Select(ToView).ToList(), filter.Page, filter.PageSize, totals.Count, totals.Expenses, totals.Incomes);
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
        row.Amount,
        row.AccountId,
        row.AccountName,
        row.CategoryId,
        row.CategoryName,
        row.DebtId,
        row.DebtName,
        row.Note,
        row.RecordedAt);

    private sealed class Row
    {
        public Guid Id { get; init; }

        public string Kind { get; init; } = string.Empty;

        public DateOnly Date { get; init; }

        public decimal Amount { get; init; }

        public Guid AccountId { get; init; }

        public string AccountName { get; init; } = string.Empty;

        public Guid CategoryId { get; init; }

        public string CategoryName { get; init; } = string.Empty;

        public Guid? DebtId { get; init; }

        public string? DebtName { get; init; }

        public string? Note { get; init; }

        public DateTimeOffset RecordedAt { get; init; }
    }

    private sealed class Totals
    {
        public int Count { get; init; }

        public decimal Expenses { get; init; }

        public decimal Incomes { get; init; }
    }
}
