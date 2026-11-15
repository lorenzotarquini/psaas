using FinHorizon.MySync.Domain.Accounts;
using FinHorizon.MySync.Domain.Common;
using FinHorizon.MySync.Domain.Currencies;
using FinHorizon.MySync.Domain.NetWorth;

namespace FinHorizon.MySync.Application.Features.Accounts;

/// <summary>
/// An account with its balance in its own <see cref="Currency"/> and, in <see cref="DisplayBalance"/>, converted into the
/// display currency (UC13); null when the rate is not at hand, for example right after creating the account.
/// </summary>
public sealed record AccountView(
    Guid Id,
    AccountKind Kind,
    string Name,
    string? Description,
    string? Tag,
    decimal InitialBalance,
    DateOnly OpenedOn,
    decimal Balance,
    PortfolioGroup Group,
    string Currency,
    decimal? DisplayBalance)
{
    public static AccountView From(Account account, Money movements, RateTable rates, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(account);
        ArgumentNullException.ThrowIfNull(rates);
        var balance = account.BalanceAfter(movements).Amount;
        return new AccountView(
            account.Id,
            account.Kind,
            account.Descriptor.Name,
            account.Descriptor.Description,
            account.Descriptor.Tag,
            account.InitialBalance.Amount,
            account.OpenedOn,
            balance,
            PortfolioScope.GroupOf(account.Kind),
            account.Currency,
            rates.Knows(account.Currency) ? RateTable.Round(rates.ToDisplay(account.Currency, balance, today)) : null);
    }
}

/// <summary>The accounts of the user and how many they may have (UC1 step 3).</summary>
public sealed record AccountList(IReadOnlyList<AccountView> Items, int Max);
