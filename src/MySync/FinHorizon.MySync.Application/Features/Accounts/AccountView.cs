using FinHorizon.MySync.Domain.Accounts;
using FinHorizon.MySync.Domain.Common;

namespace FinHorizon.MySync.Application.Features.Accounts;

public sealed record AccountView(
    Guid Id,
    AccountKind Kind,
    string Name,
    string? Description,
    string? Tag,
    decimal InitialBalance,
    DateOnly OpenedOn,
    decimal Balance)
{
    public static AccountView From(Account account, Money movements)
    {
        ArgumentNullException.ThrowIfNull(account);
        return new AccountView(
            account.Id,
            account.Kind,
            account.Descriptor.Name,
            account.Descriptor.Description,
            account.Descriptor.Tag,
            account.InitialBalance.Amount,
            account.OpenedOn,
            account.BalanceAfter(movements).Amount);
    }
}

/// <summary>The accounts of the user and how many they may have (UC1 step 3).</summary>
public sealed record AccountList(IReadOnlyList<AccountView> Items, int Max);
