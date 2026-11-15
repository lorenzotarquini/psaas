using FinHorizon.MySync.Domain.Common;

namespace FinHorizon.MySync.Domain.Transactions;

/// <summary>
/// The effect of a movement on one account: a signed amount. Balances are the initial balance plus the postings,
/// so they never depend on the kind of movement that produced them.
/// </summary>
public sealed class Posting
{
    public Posting(Guid accountId, Money amount)
    {
        AccountId = accountId;
        Amount = amount;
    }

    /// <summary>For materialization by the persistence layer only.</summary>
    private Posting()
    {
    }

    public Guid AccountId { get; private set; }

    /// <summary>Negative when money leaves the account.</summary>
    public Money Amount { get; private set; }
}
