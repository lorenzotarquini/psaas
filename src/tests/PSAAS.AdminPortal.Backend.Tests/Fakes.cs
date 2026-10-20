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

/// <summary>Stores created tenants in the read fake so the detail returned after creation is real.</summary>
public sealed class FakeTenantWriteRepository(FakeTenantReadRepository readRepository) : ITenantWriteRepository
{
    public IReadOnlyCollection<TenantProfileCreateModel> LastProfiles { get; private set; } = [];

    public IReadOnlyCollection<TenantProductiveUnitCreateModel> LastProductiveUnits { get; private set; } = [];

    /// <summary>Simulates a concurrent insert that wins the unique-index race.</summary>
    public Action? BeforeSave { get; set; }

    public Task AddWithProfilesAndProductiveUnitsAsync(
        Tenant tenant,
        IReadOnlyCollection<TenantProfileCreateModel> profiles,
        IReadOnlyCollection<TenantProductiveUnitCreateModel> productiveUnits,
        CancellationToken cancellationToken = default)
    {
        BeforeSave?.Invoke();
        LastProfiles = profiles;
        LastProductiveUnits = productiveUnits;
        readRepository.Tenants.Add(tenant);
        return Task.CompletedTask;
    }

    public Task AddAsync(Tenant tenant, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task AddWithProductiveUnitsAsync(Tenant tenant, IReadOnlyCollection<TenantProductiveUnitCreateModel> productiveUnits, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<bool> UpdateAsync(Tenant tenant, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task<bool> UpdateWithStructureAsync(
        Tenant tenant,
        IReadOnlyCollection<TenantProfileEditModel> profiles,
        IReadOnlyCollection<TenantProductiveUnitEditModel> productiveUnits,
        CancellationToken cancellationToken = default) => throw new NotSupportedException();
}

public sealed class FakeTenantUniquenessChecker : ITenantUniquenessChecker
{
    public HashSet<string> TakenValues { get; } = [];

    public Task<bool> IsCompanyNameUniqueAsync(string normalizedCompanyName, CancellationToken cancellationToken = default) =>
        Task.FromResult(!TakenValues.Contains(normalizedCompanyName));

    public Task<bool> IsCompleteVatNumberUniqueAsync(string normalizedCompleteVatNumber, CancellationToken cancellationToken = default) =>
        Task.FromResult(!TakenValues.Contains(normalizedCompleteVatNumber));

    public Task<bool> IsCertifiedEmailUniqueAsync(string normalizedCertifiedEmail, CancellationToken cancellationToken = default) =>
        Task.FromResult(!TakenValues.Contains(normalizedCertifiedEmail));
}
