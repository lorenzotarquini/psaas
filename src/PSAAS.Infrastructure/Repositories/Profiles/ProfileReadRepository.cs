using Microsoft.EntityFrameworkCore;
using NLog;
using PSAAS.Application.Abstractions.Profiles;
using PSAAS.Application.Contracts.Profiles;
using PSAAS.Infrastructure.Data.DbContexts;

namespace PSAAS.Infrastructure.Repositories.Profiles;

public sealed class ProfileReadRepository(PsaasDbContext dbContext) : IProfileReadRepository
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public async Task<IReadOnlyList<ProfileReadModel>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        Logger.Debug("Querying all profiles");
        var profiles = await dbContext.Profiles
            .AsNoTracking()
            .OrderBy(profile => profile.Name)
            .Select(profile => new ProfileReadModel(profile.Id, profile.TenantId, profile.Name, profile.Description))
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);
        Logger.Debug("Queried {ProfileCount} profiles", profiles.Length);

        return profiles;
    }

    public async Task<IReadOnlySet<string>> GetExistingIdsAsync(
        IReadOnlyCollection<string> profileIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profileIds);

        var normalizedIds = profileIds
            .Where(profileId => !string.IsNullOrWhiteSpace(profileId))
            .Select(profileId => profileId.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (normalizedIds.Length == 0)
        {
            return new HashSet<string>(StringComparer.Ordinal);
        }

        var existingIds = await dbContext.Profiles
            .AsNoTracking()
            .Where(profile => normalizedIds.Contains(profile.Id))
            .Select(profile => profile.Id)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);

        Logger.Debug("Found {ExistingCount} of {RequestedCount} profile ids", existingIds.Length, normalizedIds.Length);

        return existingIds.ToHashSet(StringComparer.Ordinal);
    }
}
