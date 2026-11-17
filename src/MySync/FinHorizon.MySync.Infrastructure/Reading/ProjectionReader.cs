using FinHorizon.MySync.Application.Abstractions.Reading;
using FinHorizon.MySync.Domain.Accounts;
using FinHorizon.MySync.Domain.Common;
using FinHorizon.MySync.Domain.NetWorth;
using FinHorizon.MySync.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FinHorizon.MySync.Infrastructure.Reading;

/// <summary>UC14-R3: the flows behind the suggested parameters, summed by Postgres per day, currency and kind.</summary>
internal sealed class ProjectionReader(MySyncDbContext db) : IProjectionReader
{
    public async Task<IReadOnlyList<ProjectionFlow>> FlowsAsync(OwnerId owner, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        // The groups come from the one mapping of the domain (UC12-R1).
        var everyday = KindsOf(PortfolioGroup.Everyday);
        var investments = KindsOf(PortfolioGroup.Investments);
        var rows = await db.Database.SqlQuery<FlowRow>($"""
            SELECT "Date", "Currency", "Kind", "DebtId", SUM("Amount") AS "Amount"
            FROM (
                SELECT t."Date", a."Currency",
                       CASE
                           WHEN t."Kind" = 'Income' THEN {(int)ProjectionFlowKind.Income}
                           WHEN t."DebtId" IS NOT NULL THEN {(int)ProjectionFlowKind.Installment}
                           WHEN t."SubscriptionId" IS NOT NULL THEN {(int)ProjectionFlowKind.SubscriptionPayment}
                           WHEN c."Fixed" THEN {(int)ProjectionFlowKind.FixedExpense}
                           ELSE {(int)ProjectionFlowKind.VariableExpense}
                       END AS "Kind",
                       t."DebtId", t."Amount"
                FROM "Transactions" t
                JOIN "Accounts" a ON a."Id" = t."AccountId"
                LEFT JOIN "Categories" c ON c."Id" = t."CategoryId"
                WHERE t."OwnerId" = {owner.Value} AND t."Kind" IN ('Income', 'Expense') AND t."Date" BETWEEN {from} AND {to}
                UNION ALL
                SELECT t."Date", a."Currency", {(int)ProjectionFlowKind.Saving}, NULL,
                       CASE WHEN a."Kind" = ANY({everyday}) THEN t."Amount" ELSE -t."Amount" END
                FROM "Transactions" t
                JOIN "Accounts" a ON a."Id" = t."AccountId"
                JOIN "Accounts" b ON b."Id" = t."CounterAccountId"
                WHERE t."OwnerId" = {owner.Value} AND t."Kind" = 'Transfer' AND t."Date" BETWEEN {from} AND {to}
                  AND ((a."Kind" = ANY({everyday}) AND b."Kind" = ANY({investments}))
                    OR (a."Kind" = ANY({investments}) AND b."Kind" = ANY({everyday})))
            ) AS flow
            GROUP BY "Date", "Currency", "Kind", "DebtId"
            """).ToListAsync(cancellationToken).ConfigureAwait(false);
        return rows.Select(row => new ProjectionFlow(row.Date, row.Currency, (ProjectionFlowKind)row.Kind, row.DebtId, row.Amount)).ToList();
    }

    private static string[] KindsOf(PortfolioGroup group) =>
        Enum.GetValues<AccountKind>().Where(kind => PortfolioScope.GroupOf(kind) == group).Select(kind => kind.ToString()).ToArray();

    private sealed class FlowRow
    {
        public DateOnly Date { get; init; }

        public string Currency { get; init; } = string.Empty;

        public int Kind { get; init; }

        public Guid? DebtId { get; init; }

        public decimal Amount { get; init; }
    }
}
