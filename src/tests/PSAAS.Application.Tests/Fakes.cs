using PSAAS.Application.Abstractions.Geography;
using PSAAS.Application.Abstractions.Tenants;
using PSAAS.Domain;

namespace PSAAS.Application.Tests;

/// <summary>Normalized values already taken, each by the tenant that owns it.</summary>
public sealed class FakeUniquenessChecker : ITenantUniquenessChecker
{
    public Dictionary<string, Guid> Taken { get; } = [];

    public Task<bool> IsCompanyNameUniqueAsync(string normalizedCompanyName, CancellationToken cancellationToken = default) =>
        Task.FromResult(!Taken.ContainsKey(normalizedCompanyName));

    public Task<bool> IsCompanyNameUniqueAsync(string normalizedCompanyName, Guid excludedTenantId, CancellationToken cancellationToken = default) =>
        Task.FromResult(IsFreeFor(normalizedCompanyName, excludedTenantId));

    public Task<bool> IsCompleteVatNumberUniqueAsync(string normalizedCompleteVatNumber, CancellationToken cancellationToken = default) =>
        Task.FromResult(!Taken.ContainsKey(normalizedCompleteVatNumber));

    public Task<bool> IsCompleteVatNumberUniqueAsync(string normalizedCompleteVatNumber, Guid excludedTenantId, CancellationToken cancellationToken = default) =>
        Task.FromResult(IsFreeFor(normalizedCompleteVatNumber, excludedTenantId));

    public Task<bool> IsCertifiedEmailUniqueAsync(string normalizedCertifiedEmail, CancellationToken cancellationToken = default) =>
        Task.FromResult(!Taken.ContainsKey(normalizedCertifiedEmail));

    public Task<bool> IsCertifiedEmailUniqueAsync(string normalizedCertifiedEmail, Guid excludedTenantId, CancellationToken cancellationToken = default) =>
        Task.FromResult(IsFreeFor(normalizedCertifiedEmail, excludedTenantId));

    private bool IsFreeFor(string value, Guid tenantId) => !Taken.TryGetValue(value, out var owner) || owner == tenantId;
}

public sealed class FakeCityList : IItalianCityProvider
{
    public List<ItalianCity> Cities { get; } = [new("Roma", "RM", ["00118", "00100"])];

    public bool Unavailable { get; set; }

    public Task<IReadOnlyList<ItalianCity>?> SearchAsync(string term, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<ItalianCity>?>(Unavailable
            ? null
            : Cities.Where(city => city.Name.Contains(term, StringComparison.OrdinalIgnoreCase)).ToArray());
}

/// <summary>Records what the use case asked to persist.</summary>
public sealed class FakeTenantWriteRepository : ITenantWriteRepository
{
    public Tenant? Saved { get; private set; }

    public IReadOnlyCollection<TenantProfileCreateModel> SavedProfiles { get; private set; } = [];

    public IReadOnlyCollection<TenantProductiveUnitCreateModel> SavedProductiveUnits { get; private set; } = [];

    /// <summary>Runs before saving: lets a test simulate a concurrent insert winning the race.</summary>
    public Action? BeforeSave { get; set; }

    public Task AddWithProfilesAndProductiveUnitsAsync(
        Tenant tenant,
        IReadOnlyCollection<TenantProfileCreateModel> profiles,
        IReadOnlyCollection<TenantProductiveUnitCreateModel> productiveUnits,
        CancellationToken cancellationToken = default)
    {
        BeforeSave?.Invoke();
        (Saved, SavedProfiles, SavedProductiveUnits) = (tenant, profiles, productiveUnits);
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
