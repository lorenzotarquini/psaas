using FinHorizon.MySync.Application.Abstractions;
using FinHorizon.MySync.Application.Abstractions.Limits;
using FinHorizon.MySync.Domain.Common;
using Microsoft.Extensions.Options;

namespace FinHorizon.MySync.Application.Features.Memberships;

/// <summary>
/// Contract C2 and C6: the limits of the tier paid. Without a tier (read-only, C4) the lowest one is shown; a limit the
/// tier does not declare falls back to configuration (MySync:MaxAccounts, MaxDebts, MaxSubscriptions).
/// </summary>
public sealed class TierResourceLimits(MembershipSync memberships, IModuleTiers tiers, IOptions<LimitsOptions> options) : IResourceLimits
{
    public async Task<UserLimits> ForAsync(OwnerId owner, CancellationToken cancellationToken = default)
    {
        var entitlement = await memberships.EntitlementAsync(owner, cancellationToken).ConfigureAwait(false);
        var tier = entitlement.Tier ?? tiers.Tiers.MinBy(item => item.Level);
        int Limit(string key, int fallback) => tier?.Limits is { } limits && limits.TryGetValue(key, out var max) ? max : fallback;
        var configured = options.Value;
        return new UserLimits(
            Limit(MySyncModule.AccountsLimit, configured.MaxAccounts),
            Limit(MySyncModule.DebtsLimit, configured.MaxDebts),
            Limit(MySyncModule.SubscriptionsLimit, configured.MaxSubscriptions));
    }
}
