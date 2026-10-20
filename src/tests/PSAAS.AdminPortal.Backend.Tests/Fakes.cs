using PSAAS.Application.Abstractions.Geography;
using PSAAS.Application.Abstractions.Profiles;
using PSAAS.Application.Abstractions.Tenants;
using PSAAS.Application.Abstractions.Users;
using PSAAS.Application.Contracts;
using PSAAS.Application.Contracts.Profiles;
using PSAAS.Application.Contracts.Users;
using PSAAS.Domain;

namespace PSAAS.AdminPortal.Backend.Tests;

public sealed class FakeTenantReadRepository : ITenantReadRepository
{
    public List<Tenant> Tenants { get; } = [];

    public Dictionary<string, TenantStructureReadModel> Structures { get; } = [];

    public (string? Search, int Page, int PageSize) LastPageRequest { get; private set; }

    public Task<PagedResult<Tenant>> GetPageAsync(string? search, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        LastPageRequest = (search, page, pageSize);
        return Task.FromResult(new PagedResult<Tenant>(Tenants, Tenants.Count, page, pageSize));
    }

    public Task<Tenant?> GetByIdAsync(Guid tenantId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Tenants.SingleOrDefault(tenant => tenant.Id == tenantId));

    public Task<TenantStructureReadModel> GetProfilesAndProductiveUnitsAsync(string tenantId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Structures.GetValueOrDefault(tenantId) ?? new TenantStructureReadModel([], []));
}

public sealed class FakeProfileReadRepository : IProfileReadRepository
{
    public List<ProfileReadModel> Profiles { get; } = [];

    public Task<IReadOnlyList<ProfileReadModel>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ProfileReadModel>>(Profiles);

    public Task<IReadOnlySet<string>> GetExistingIdsAsync(IReadOnlyCollection<string> profileIds, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlySet<string>>(Profiles.Select(profile => profile.Id).Intersect(profileIds).ToHashSet());
}

public sealed class FakeUserReadRepository : IUserReadRepository
{
    public List<UserReadModel> Users { get; } = [];

    public Task<IReadOnlyList<UserReadModel>> GetByTenantAsync(string tenantId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<UserReadModel>>(Users.Where(user => user.TenantId == tenantId).ToArray());

    public Task<UserReadModel?> GetByIdAsync(string tenantId, Guid userId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Users.SingleOrDefault(user => user.TenantId == tenantId && user.Id == userId));

    public Task<UserReadModel?> GetByKeycloakIdAsync(string keycloakId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Users.SingleOrDefault(user => user.KeycloakId == keycloakId));
}

public sealed class FakeItalianCityProvider : IItalianCityProvider
{
    public IReadOnlyList<ItalianCity>? Result { get; set; } = [];

    public Task<IReadOnlyList<ItalianCity>?> SearchAsync(string term, CancellationToken ct) => Task.FromResult(Result);
}
