using FinHorizon.MySync.Domain.Common;
using Microsoft.Extensions.Options;

namespace FinHorizon.MySync.Application.Abstractions.Limits;

/// <summary>The same limits for every user, from configuration.</summary>
public sealed class ConfiguredResourceLimits(IOptions<LimitsOptions> options) : IResourceLimits
{
    public Task<UserLimits> ForAsync(OwnerId owner, CancellationToken cancellationToken = default) =>
        Task.FromResult(new UserLimits(options.Value.MaxAccounts, options.Value.MaxDebts, options.Value.MaxSubscriptions));
}
