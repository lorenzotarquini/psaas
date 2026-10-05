using PSAAS.AdminPortal.Features.Tenants.Services;
using PSAAS.Application.Abstractions.Tenants;
using PSAAS.Application.Contracts.Tenants;
using PSAAS.Application.Validators.Tenants;
using PSAAS.Domain;
using Xunit;

namespace PSAAS.AdminPortal.Tests;

public sealed class TenantEditServiceTests
{
    private readonly FakeTenantReadRepository _readRepository = new();
    private readonly FakeTenantWriteRepository _writeRepository = new();
    private readonly ConfigurableTenantUniquenessChecker _uniquenessChecker = new();

    private TenantEditService CreateService() =>
        new(new TenantCreateValidator(_uniquenessChecker), _readRepository, _writeRepository);

    [Fact]
    public async Task GetTenantForEditAsync_ReturnsEditableProjection_WithoutExposingDomain()
    {
        var tenant = CreateExistingTenant();
        _readRepository.Tenants[tenant.Id] = tenant;
        var service = CreateService();

        var result = await service.GetTenantForEditAsync(tenant.Id);

        Assert.NotNull(result);
        Assert.Equal(tenant.Id, result.Id);
        Assert.Equal("Acme S.r.l.", result.CompanyName);
        Assert.Equal("IT12345678901", result.CompleteVatNumber);
        Assert.Equal("pec@example.test", result.CertifiedEmail);
        Assert.Equal("Via Roma 1", result.Street);
    }

    [Fact]
    public async Task UpdateTenantAsync_PersistsNormalizedTenant_WhenRequestIsValid()
    {
        var tenant = CreateExistingTenant();
        var service = CreateService();

        var result = await service.UpdateTenantAsync(new TenantEditRequest(
            tenant.Id,
            " NewCo ",
            " it10987654321 ",
            " new@example.test ",
            " Via Milano 2 ",
            " Milano ",
            " 20100 ",
            " Italia "));

        Assert.True(result.Succeeded);
        var updated = Assert.Single(_writeRepository.UpdatedTenants);
        Assert.Equal(tenant.Id, updated.Id);
        Assert.Equal("NewCo", updated.CompanyName);
        Assert.Equal("10987654321", updated.VatNumber);
        Assert.Equal("IT10987654321", updated.CompleteVatNumber);
        Assert.Equal("new@example.test", updated.CertifiedEmail);
        Assert.Equal(new Address("Via Milano 2", "Milano", "20100", "Italia"), updated.Address);
    }

    [Fact]
    public async Task UpdateTenantAsync_DoesNotTreatCurrentTenantAsDuplicate_WhenUniqueFieldsAreUnchanged()
    {
        var tenant = CreateExistingTenant();
        _uniquenessChecker.CompanyNames[TenantFieldNormalizer.NormalizeCompanyName(tenant.CompanyName)] = tenant.Id;
        _uniquenessChecker.CompleteVatNumbers[tenant.CompleteVatNumber] = tenant.Id;
        _uniquenessChecker.CertifiedEmails[TenantFieldNormalizer.NormalizeCertifiedEmail(tenant.CertifiedEmail)] = tenant.Id;
        var service = CreateService();

        var result = await service.UpdateTenantAsync(ToRequest(tenant));

        Assert.True(result.Succeeded);
        Assert.Null(result.CompanyNameErrorCode);
        Assert.Null(result.CompleteVatNumberErrorCode);
        Assert.Null(result.CertifiedEmailErrorCode);
        Assert.Single(_writeRepository.UpdatedTenants);
    }

    [Fact]
    public async Task UpdateTenantAsync_RejectsDuplicateUniqueFields_FromAnotherTenant()
    {
        var tenant = CreateExistingTenant();
        _uniquenessChecker.CompleteVatNumbers[tenant.CompleteVatNumber] = Guid.NewGuid();
        var service = CreateService();

        var result = await service.UpdateTenantAsync(ToRequest(tenant));

        Assert.False(result.Succeeded);
        Assert.Equal(TenantFieldValidationErrorCodes.NotUnique, result.CompleteVatNumberErrorCode);
        Assert.Empty(_writeRepository.UpdatedTenants);
    }

    [Fact]
    public async Task UpdateTenantAsync_ReturnsRequiredErrors_WithoutPersisting_WhenFieldsAreMissing()
    {
        var service = CreateService();

        var result = await service.UpdateTenantAsync(new TenantEditRequest(
            Guid.NewGuid(), " ", null, "", null, "  ", "", ""));

        Assert.False(result.Succeeded);
        Assert.Equal(TenantFieldValidationErrorCodes.Required, result.CompanyNameErrorCode);
        Assert.Equal(TenantFieldValidationErrorCodes.Required, result.CompleteVatNumberErrorCode);
        Assert.Equal(TenantFieldValidationErrorCodes.Required, result.CertifiedEmailErrorCode);
        Assert.Equal(TenantFieldValidationErrorCodes.Required, result.StreetErrorCode);
        Assert.Equal(TenantFieldValidationErrorCodes.Required, result.CityErrorCode);
        Assert.Equal(TenantFieldValidationErrorCodes.Required, result.PostalCodeErrorCode);
        Assert.Equal(TenantFieldValidationErrorCodes.Required, result.CountryErrorCode);
        Assert.Empty(_writeRepository.UpdatedTenants);
    }

    [Fact]
    public async Task UpdateTenantAsync_ReportsNotUnique_WhenConcurrentUpdateWinsUniqueIndex()
    {
        var tenant = CreateExistingTenant();
        _writeRepository.OnUpdate = updated =>
            _uniquenessChecker.CompleteVatNumbers[updated.CompleteVatNumber] = Guid.NewGuid();
        _writeRepository.ExceptionToThrow = new TenantUniquenessConflictException("unique index race");
        var service = CreateService();

        var result = await service.UpdateTenantAsync(ToRequest(tenant));

        Assert.False(result.Succeeded);
        Assert.Equal(TenantFieldValidationErrorCodes.NotUnique, result.CompleteVatNumberErrorCode);
        Assert.Null(result.ErrorCode);
        Assert.Single(_writeRepository.UpdatedTenants);
    }

    [Fact]
    public async Task UpdateTenantAsync_ReturnsNotFound_WhenRepositoryCannotFindTenant()
    {
        _writeRepository.UpdateResult = false;
        var service = CreateService();

        var result = await service.UpdateTenantAsync(ToRequest(CreateExistingTenant()));

        Assert.False(result.Succeeded);
        Assert.Equal(TenantEditErrorCodes.NotFound, result.ErrorCode);
    }

    private static Tenant CreateExistingTenant() =>
        Tenant.Rehydrate(
            Guid.NewGuid(),
            "Acme S.r.l.",
            "12345678901",
            "IT12345678901",
            "pec@example.test",
            new Address("Via Roma 1", "Roma", "00100", "Italia"));

    private static TenantEditRequest ToRequest(Tenant tenant) =>
        new(
            tenant.Id,
            tenant.CompanyName,
            tenant.CompleteVatNumber,
            tenant.CertifiedEmail,
            tenant.Address.street,
            tenant.Address.city,
            tenant.Address.postalCode,
            tenant.Address.country);

    private sealed class FakeTenantReadRepository : ITenantReadRepository
    {
        public Dictionary<Guid, Tenant> Tenants { get; } = [];

        public Task<IReadOnlyList<Tenant>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Tenant>>(Tenants.Values.ToArray());

        public Task<Tenant?> GetByIdAsync(Guid tenantId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Tenants.GetValueOrDefault(tenantId));
    }

    private sealed class FakeTenantWriteRepository : ITenantWriteRepository
    {
        public List<Tenant> UpdatedTenants { get; } = [];

        public bool UpdateResult { get; set; } = true;

        public Action<Tenant>? OnUpdate { get; set; }

        public Exception? ExceptionToThrow { get; set; }

        public Task AddAsync(Tenant tenant, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> UpdateAsync(Tenant tenant, CancellationToken cancellationToken = default)
        {
            UpdatedTenants.Add(tenant);
            OnUpdate?.Invoke(tenant);

            if (ExceptionToThrow is not null)
            {
                throw ExceptionToThrow;
            }

            return Task.FromResult(UpdateResult);
        }
    }

    private sealed class ConfigurableTenantUniquenessChecker : ITenantUniquenessChecker
    {
        public Dictionary<string, Guid> CompanyNames { get; } = new(StringComparer.Ordinal);

        public Dictionary<string, Guid> CompleteVatNumbers { get; } = new(StringComparer.Ordinal);

        public Dictionary<string, Guid> CertifiedEmails { get; } = new(StringComparer.Ordinal);

        public Task<bool> IsCompanyNameUniqueAsync(string normalizedCompanyName, CancellationToken cancellationToken = default) =>
            Task.FromResult(!CompanyNames.ContainsKey(normalizedCompanyName));

        public Task<bool> IsCompanyNameUniqueAsync(
            string normalizedCompanyName,
            Guid excludedTenantId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(!CompanyNames.TryGetValue(normalizedCompanyName, out var tenantId) || tenantId == excludedTenantId);

        public Task<bool> IsCompleteVatNumberUniqueAsync(string normalizedCompleteVatNumber, CancellationToken cancellationToken = default) =>
            Task.FromResult(!CompleteVatNumbers.ContainsKey(normalizedCompleteVatNumber));

        public Task<bool> IsCompleteVatNumberUniqueAsync(
            string normalizedCompleteVatNumber,
            Guid excludedTenantId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(!CompleteVatNumbers.TryGetValue(normalizedCompleteVatNumber, out var tenantId) || tenantId == excludedTenantId);

        public Task<bool> IsCertifiedEmailUniqueAsync(string normalizedCertifiedEmail, CancellationToken cancellationToken = default) =>
            Task.FromResult(!CertifiedEmails.ContainsKey(normalizedCertifiedEmail));

        public Task<bool> IsCertifiedEmailUniqueAsync(
            string normalizedCertifiedEmail,
            Guid excludedTenantId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(!CertifiedEmails.TryGetValue(normalizedCertifiedEmail, out var tenantId) || tenantId == excludedTenantId);
    }
}
