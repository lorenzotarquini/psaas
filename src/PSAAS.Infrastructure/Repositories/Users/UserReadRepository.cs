using Microsoft.EntityFrameworkCore;
using NLog;
using PSAAS.Application.Abstractions.Users;
using PSAAS.Application.Contracts.Users;
using PSAAS.Infrastructure.Data.DbContexts;
using PSAAS.Infrastructure.Data.DbModels;

namespace PSAAS.Infrastructure.Repositories.Users;

public sealed class UserReadRepository(PsaasDbContext dbContext) : IUserReadRepository
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public async Task<IReadOnlyList<UserReadModel>> GetByTenantAsync(
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        Logger.Debug("Querying users of tenant {TenantId}", tenantId);
        return await Project(dbContext.Users.Where(user => user.TenantId == tenantId))
            .OrderBy(user => user.LastName)
            .ThenBy(user => user.FirstName)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public Task<UserReadModel?> GetByIdAsync(
        string tenantId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        Logger.Debug("Querying user {UserId} of tenant {TenantId}", userId, tenantId);
        return Project(dbContext.Users.Where(user => user.TenantId == tenantId && user.Id == userId))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public Task<UserReadModel?> GetByKeycloakIdAsync(string keycloakId, CancellationToken cancellationToken = default)
    {
        Logger.Debug("Querying user by Keycloak id {KeycloakId}", keycloakId);
        return Project(dbContext.Users.Where(user => user.KeycloakId == keycloakId))
            .SingleOrDefaultAsync(cancellationToken);
    }

    private static IQueryable<UserReadModel> Project(IQueryable<UserDbModel> users) =>
        users.AsNoTracking().Select(user => new UserReadModel(
            user.Id,
            user.KeycloakId,
            user.UserName,
            user.Email,
            user.FirstName,
            user.LastName,
            user.TenantId,
            user.ProfileId,
            user.Profile == null ? null : user.Profile.Name));
}
