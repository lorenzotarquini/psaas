using FinHorizon.MySync.Domain.Accounts;
using FinHorizon.MySync.Domain.Debts;

namespace FinHorizon.MySync.Domain.NetWorth;

/// <summary>
/// Which groups a view of the portfolio counts (UC12). Day-to-day items are always in; stable assets with long-term
/// debts, and investments with deposits, each have their own switch. The mapping kind → group lives here only.
/// </summary>
public sealed class PortfolioScope
{
    private static readonly Dictionary<AccountKind, PortfolioGroup> AccountGroups = new()
    {
        [AccountKind.Valuables] = PortfolioGroup.Stable,
        [AccountKind.DepositAccount] = PortfolioGroup.Investments,
        [AccountKind.Investments] = PortfolioGroup.Investments,
        [AccountKind.SavingsPlan] = PortfolioGroup.Investments,
        [AccountKind.VirtualCurrency] = PortfolioGroup.Investments,
    };

    private static readonly Dictionary<DebtKind, PortfolioGroup> DebtGroups = new()
    {
        [DebtKind.Mortgage] = PortfolioGroup.Stable,
    };

    private readonly HashSet<PortfolioGroup> _included;

    private PortfolioScope(IEnumerable<PortfolioGroup> included)
    {
        _included = [PortfolioGroup.Everyday, .. included];
        ExcludedAccountKinds = Enum.GetValues<AccountKind>().Where(kind => !Includes(kind)).ToHashSet();
        ExcludedDebtKinds = Enum.GetValues<DebtKind>().Where(kind => !Includes(kind)).ToHashSet();
    }

    public static PortfolioScope Everything { get; } = For(includeStable: true, includeInvestments: true);

    public IReadOnlySet<AccountKind> ExcludedAccountKinds { get; }

    public IReadOnlySet<DebtKind> ExcludedDebtKinds { get; }

    public static PortfolioScope For(bool includeStable, bool includeInvestments)
    {
        var included = new List<PortfolioGroup>();
        if (includeStable)
        {
            included.Add(PortfolioGroup.Stable);
        }

        if (includeInvestments)
        {
            included.Add(PortfolioGroup.Investments);
        }

        return new PortfolioScope(included);
    }

    public static PortfolioGroup GroupOf(AccountKind kind) => AccountGroups.GetValueOrDefault(kind, PortfolioGroup.Everyday);

    public static PortfolioGroup GroupOf(DebtKind kind) => DebtGroups.GetValueOrDefault(kind, PortfolioGroup.Everyday);

    public bool Includes(AccountKind kind) => _included.Contains(GroupOf(kind));

    public bool Includes(DebtKind kind) => _included.Contains(GroupOf(kind));
}
