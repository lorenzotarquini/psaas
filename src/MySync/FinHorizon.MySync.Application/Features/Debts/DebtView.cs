using FinHorizon.MySync.Domain.Common;
using FinHorizon.MySync.Domain.Currencies;
using FinHorizon.MySync.Domain.Debts;
using FinHorizon.MySync.Domain.NetWorth;

namespace FinHorizon.MySync.Application.Features.Debts;

/// <summary>A debt with its residual in its own <see cref="Currency"/> and converted into the display currency (UC13), as for accounts.</summary>
public sealed record DebtView(
    Guid Id,
    DebtKind Kind,
    string Name,
    string? Description,
    string? Tag,
    Guid AccountId,
    decimal InitialResidual,
    decimal Residual,
    DateOnly CreatedOn,
    bool PaidOff,
    PortfolioGroup Group,
    string Currency,
    decimal? DisplayResidual)
{
    public static DebtView From(Debt debt, Money installmentsPaid, RateTable rates, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(debt);
        ArgumentNullException.ThrowIfNull(rates);
        var residual = debt.ResidualAfter(installmentsPaid);
        return new DebtView(
            debt.Id,
            debt.Kind,
            debt.Descriptor.Name,
            debt.Descriptor.Description,
            debt.Descriptor.Tag,
            debt.AccountId,
            debt.InitialResidual.Amount,
            residual.Amount,
            debt.CreatedOn,
            !residual.IsPositive,
            PortfolioScope.GroupOf(debt.Kind),
            debt.Currency,
            rates.Knows(debt.Currency) ? RateTable.Round(rates.ToDisplay(debt.Currency, residual.Amount, today)) : null);
    }
}

/// <summary>The debts of the user and how many they may have; 0 means debts are off (UC3-R5).</summary>
public sealed record DebtList(IReadOnlyList<DebtView> Items, int Max);
