using FinHorizon.MySync.Domain.Common;
using FinHorizon.MySync.Domain.Debts;
using FinHorizon.MySync.Domain.NetWorth;

namespace FinHorizon.MySync.Application.Features.Debts;

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
    PortfolioGroup Group)
{
    public static DebtView From(Debt debt, Money installmentsPaid)
    {
        ArgumentNullException.ThrowIfNull(debt);
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
            PortfolioScope.GroupOf(debt.Kind));
    }
}

/// <summary>The debts of the user and how many they may have; 0 means debts are off (UC3-R5).</summary>
public sealed record DebtList(IReadOnlyList<DebtView> Items, int Max);
