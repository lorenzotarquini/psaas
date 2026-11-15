using FinHorizon.MySync.Application.Abstractions.Reading;
using FinHorizon.MySync.Domain.Common;
using FinHorizon.MySync.Domain.NetWorth;
using FinHorizon.MySync.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FinHorizon.MySync.Infrastructure.Reading;

/// <summary>The sums behind balances, residuals and the trend, computed by Postgres over one owner's rows.</summary>
internal sealed class LedgerReader(MySyncDbContext db) : ILedgerReader
{
    public async Task<IReadOnlyDictionary<Guid, Money>> MovementsByAccountAsync(OwnerId owner, CancellationToken cancellationToken = default)
    {
        var rows = await db.Database.SqlQuery<KeyedAmount>($"""
            SELECT p."AccountId" AS "Key", SUM(p."Amount") AS "Amount"
            FROM "Postings" p
            JOIN "Transactions" t ON t."Id" = p."TransactionId"
            WHERE t."OwnerId" = {owner.Value}
            GROUP BY p."AccountId"
            """).ToListAsync(cancellationToken).ConfigureAwait(false);
        return rows.ToDictionary(row => row.Key, row => new Money(row.Amount));
    }

    public async Task<IReadOnlyDictionary<Guid, Money>> InstallmentsByDebtAsync(OwnerId owner, CancellationToken cancellationToken = default)
    {
        var rows = await db.Database.SqlQuery<KeyedAmount>($"""
            SELECT "DebtId" AS "Key", SUM("Amount") AS "Amount"
            FROM "Transactions"
            WHERE "OwnerId" = {owner.Value} AND "DebtId" IS NOT NULL
            GROUP BY "DebtId"
            """).ToListAsync(cancellationToken).ConfigureAwait(false);
        return rows.ToDictionary(row => row.Key, row => new Money(row.Amount));
    }

    public async Task<Money> InstallmentsPaidAsync(OwnerId owner, Guid debtId, Guid? exceptTransactionId, CancellationToken cancellationToken = default)
    {
        // Guid.Empty never matches a row: it stands for "leave nothing out".
        var except = exceptTransactionId ?? Guid.Empty;
        var paid = await db.Database.SqlQuery<decimal>($"""
            SELECT COALESCE(SUM("Amount"), 0) AS "Value"
            FROM "Transactions"
            WHERE "OwnerId" = {owner.Value} AND "DebtId" = {debtId} AND "Id" <> {except}
            """).SingleAsync(cancellationToken).ConfigureAwait(false);
        return new Money(paid);
    }

    public async Task<LedgerHistory> HistoryAsync(OwnerId owner, CancellationToken cancellationToken = default)
    {
        var accounts = await db.Accounts.AsNoTracking()
            .Where(account => account.OwnerId == owner)
            .Select(account => new OpeningValue(account.OpenedOn, account.InitialBalance))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        var debts = await db.Debts.AsNoTracking()
            .Where(debt => debt.OwnerId == owner)
            .Select(debt => new OpeningValue(debt.CreatedOn, debt.InitialResidual))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        var days = await db.Database.SqlQuery<DailyRow>($"""
            SELECT "Date", SUM("BalanceChange") AS "BalanceChange", SUM("InstallmentsPaid") AS "InstallmentsPaid"
            FROM (
                SELECT t."Date", p."Amount" AS "BalanceChange", 0 AS "InstallmentsPaid"
                FROM "Postings" p
                JOIN "Transactions" t ON t."Id" = p."TransactionId"
                WHERE t."OwnerId" = {owner.Value}
                UNION ALL
                SELECT "Date", 0, "Amount"
                FROM "Transactions"
                WHERE "OwnerId" = {owner.Value} AND "DebtId" IS NOT NULL
            ) AS day
            GROUP BY "Date"
            """).ToListAsync(cancellationToken).ConfigureAwait(false);
        return new LedgerHistory(
            accounts,
            debts,
            days.Select(day => new DailyMovement(day.Date, new Money(day.BalanceChange), new Money(day.InstallmentsPaid))).ToList());
    }

    private sealed class KeyedAmount
    {
        public Guid Key { get; init; }

        public decimal Amount { get; init; }
    }

    private sealed class DailyRow
    {
        public DateOnly Date { get; init; }

        public decimal BalanceChange { get; init; }

        public decimal InstallmentsPaid { get; init; }
    }
}
