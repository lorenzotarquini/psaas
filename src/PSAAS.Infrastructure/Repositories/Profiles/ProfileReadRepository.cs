using Microsoft.EntityFrameworkCore;
using NLog;
using PSAAS.Application.Abstractions.Profiles;
using PSAAS.Application.Contracts.Profiles;
using PSAAS.Application.Contracts.Users;
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
            .Join(
                dbContext.Tenants,
                profile => profile.TenantId,
                tenant => tenant.Id,
                (profile, tenant) => new { profile, tenant })
            .OrderBy(row => row.profile.Name)
            .Select(row => new ProfileReadModel(
                row.profile.Id, row.tenant.Id, row.tenant.CompanyName, row.profile.Name, row.profile.Description))
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);
        Logger.Debug("Queried {ProfileCount} profiles", profiles.Length);

        return profiles;
    }

    public async Task<IReadOnlyList<UserProfileReadModel>> GetWithTenantAsync(
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
            return [];
        }

        var profiles = await dbContext.Profiles
            .AsNoTracking()
            .Where(profile => normalizedIds.Contains(profile.Id))
            .Join(
                dbContext.Tenants,
                profile => profile.TenantId,
                tenant => tenant.Id,
                (profile, tenant) => new UserProfileReadModel(profile.Id, profile.Name, tenant.Id, tenant.CompanyName, tenant.Status))
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);

        Logger.Debug("Found {ExistingCount} of {RequestedCount} profile ids", profiles.Length, normalizedIds.Length);

        return profiles;
    }
}
