using FinHorizon.MySync.Domain.Common;

namespace FinHorizon.MySync.Application.Abstractions.Limits;

/// <summary>How many accounts and debts a user may have (RF2, RF4).</summary>
public sealed record UserLimits(int MaxAccounts, int MaxDebts);

/// <summary>
/// Source of the limits. Today they come from configuration; a later phase can read them from the user's plan
/// by replacing this one implementation.
/// </summary>
public interface IResourceLimits
{
    Task<UserLimits> ForAsync(OwnerId owner, CancellationToken cancellationToken = default);
}
