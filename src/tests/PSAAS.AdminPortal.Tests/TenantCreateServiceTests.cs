using PSAAS.AdminPortal.Features.Tenants.Services;
using PSAAS.Application.Abstractions.Tenants;
using PSAAS.Application.Contracts.Tenants;
using PSAAS.Application.Validators.Tenants;
using PSAAS.Domain;
using Xunit;

namespace PSAAS.AdminPortal.Tests;

public sealed class TenantCreateServiceTests
{
    private readonly FakeTenantWriteRepository _writeRepository = new();
    private readonly ConfigurableTenantUniquenessChecker _uniquenessChecker = new();

    private TenantCreateService CreateService() =>
        new(new TenantCreateValidator(_uniquenessChecker), _writeRepository);

    private static TenantCreateRequest CreateValidRequest() => new(
        CompanyName: " Acme S.r.l. ",
        CompleteVatNumber: " it12345678901 ",
        CertifiedEmail: " Pec@Example.test ",
        Street: " Via Roma 1 ",
        City: " Roma ",
        PostalCode: " 00100 ",
        Country: "Italia");

    [Fact]
    public async Task CreateTenantAsync_PersistsNormalizedTenant_WhenRequestIsValid()
    {
        var service = CreateService();

        var result = await service.CreateTenantAsync(CreateValidRequest());

        Assert.True(result.Succeeded);
        Assert.Null(result.ErrorCode);

        var tenant = Assert.Single(_writeRepository.AddedTenants);
        Assert.NotEqual(Guid.Empty, tenant.Id);
        Assert.Equal("Acme S.r.l.", tenant.CompanyName);
        Assert.Equal("12345678901", tenant.VatNumber);
        Assert.Equal("IT12345678901", tenant.CompleteVatNumber);
        Assert.Equal("Pec@Example.test", tenant.CertifiedEmail);
        Assert.Equal(new Address("Via Roma 1", "Roma", "00100", "Italia"), tenant.Address);
    }

    [Fact]
    public async Task CreateTenantAsync_ReturnsRequiredErrors_WithoutPersisting_WhenFieldsAreMissing()
    {
        var service = CreateService();

        var result = await service.CreateTenantAsync(new TenantCreateRequest(
            CompanyName: " ",
            CompleteVatNumber: null,
            CertifiedEmail: "",
            Street: null,
            City: "  ",
            PostalCode: "",
            Country: ""));

        Assert.False(result.Succeeded);
        Assert.Equal(TenantFieldValidationErrorCodes.Required, result.CompanyNameErrorCode);
        Assert.Equal(TenantFieldValidationErrorCodes.Required, result.CompleteVatNumberErrorCode);
        Assert.Equal(TenantFieldValidationErrorCodes.Required, result.CertifiedEmailErrorCode);
        Assert.Equal(TenantFieldValidationErrorCodes.Required, result.StreetErrorCode);
        Assert.Equal(TenantFieldValidationErrorCodes.Required, result.CityErrorCode);
        Assert.Equal(TenantFieldValidationErrorCodes.Required, result.PostalCodeErrorCode);
        Assert.Equal(TenantFieldValidationErrorCodes.Required, result.CountryErrorCode);
        Assert.Empty(_writeRepository.AddedTenants);
    }

    [Fact]
    public async Task CreateTenantAsync_RejectsInvalidCompleteVatNumberFormat_WithoutPersisting()
    {
        var service = CreateService();

        var result = await service.CreateTenantAsync(
            CreateValidRequest() with { CompleteVatNumber = "IT123" });

        Assert.False(result.Succeeded);
        Assert.Equal(TenantFieldValidationErrorCodes.InvalidFormat, result.CompleteVatNumberErrorCode);
        Assert.Null(result.CompanyNameErrorCode);
        Assert.Empty(_writeRepository.AddedTenants);
    }

    [Fact]
    public async Task CreateTenantAsync_RejectsCompanyNameLongerThanTwentyDisplayCharacters_WithoutPersisting()
    {
        var service = CreateService();

        var result = await service.CreateTenantAsync(
            CreateValidRequest() with { CompanyName = "123456789012345678901" });

        Assert.False(result.Succeeded);
        Assert.Equal(TenantFieldValidationErrorCodes.MaxLengthExceeded, result.CompanyNameErrorCode);
        Assert.Null(result.CertifiedEmailErrorCode);
        Assert.Empty(_writeRepository.AddedTenants);
    }

    [Fact]
    public async Task CreateTenantAsync_RejectsInvalidCertifiedEmailFormat_WithoutPersisting()
    {
        var service = CreateService();

        var result = await service.CreateTenantAsync(
            CreateValidRequest() with { CertifiedEmail = "not-an-email" });

        Assert.False(result.Succeeded);
        Assert.Equal(TenantFieldValidationErrorCodes.InvalidFormat, result.CertifiedEmailErrorCode);
        Assert.Null(result.CompanyNameErrorCode);
        Assert.Empty(_writeRepository.AddedTenants);
    }

    [Fact]
    public async Task CreateTenantAsync_RejectsNonUniqueValues_WithoutPersisting()
    {
        _uniquenessChecker.OccupiedCompleteVatNumbers.Add("IT12345678901");
        var service = CreateService();

        var result = await service.CreateTenantAsync(CreateValidRequest());

        Assert.False(result.Succeeded);
        Assert.Equal(TenantFieldValidationErrorCodes.NotUnique, result.CompleteVatNumberErrorCode);
        Assert.Null(result.CompanyNameErrorCode);
        Assert.Null(result.CertifiedEmailErrorCode);
        Assert.Empty(_writeRepository.AddedTenants);
    }

    [Fact]
    public async Task CreateTenantAsync_ReportsNotUnique_WhenConcurrentInsertWinsUniqueIndex()
    {
        // Simulates the unique-index race: all values pass the pre-insert
        // validation, but a concurrent insert takes the complete VAT number
        // before the write repository completes.
        _writeRepository.OnAdd = tenant =>
            _uniquenessChecker.OccupiedCompleteVatNumbers.Add(tenant.CompleteVatNumber);
        _writeRepository.ExceptionToThrow = new TenantUniquenessConflictException("unique index race");
        var service = CreateService();

        var result = await service.CreateTenantAsync(CreateValidRequest());

        Assert.False(result.Succeeded);
        Assert.Equal(TenantFieldValidationErrorCodes.NotUnique, result.CompleteVatNumberErrorCode);
        Assert.Null(result.CompanyNameErrorCode);
        Assert.Null(result.CertifiedEmailErrorCode);
        // The conflict is surfaced as a field error, not as a generic failure.
        Assert.Null(result.ErrorCode);
        Assert.Single(_writeRepository.AddedTenants);
    }

    [Fact]
    public async Task CreateTenantAsync_ReturnsSaveFailed_WhenPersistenceFails()
    {
        _writeRepository.ExceptionToThrow = new InvalidOperationException("database unavailable");
        var service = CreateService();

        var result = await service.CreateTenantAsync(CreateValidRequest());

        Assert.False(result.Succeeded);
        Assert.Equal(TenantCreateErrorCodes.SaveFailed, result.ErrorCode);
        Assert.Null(result.CompanyNameErrorCode);
    }

    [Fact]
    public async Task CreateTenantAsync_Rethrows_WhenCancelled()
    {
        _uniquenessChecker.ThrowWhenCancelled = true;
        var service = CreateService();
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => service.CreateTenantAsync(CreateValidRequest(), cancellation.Token));
    }

    private sealed class FakeTenantWriteRepository : ITenantWriteRepository
    {
        public List<Tenant> AddedTenants { get; } = [];

        public Action<Tenant>? OnAdd { get; set; }

        public Exception? ExceptionToThrow { get; set; }

        public Task AddAsync(Tenant tenant, CancellationToken cancellationToken = default)
        {
            AddedTenants.Add(tenant);
            OnAdd?.Invoke(tenant);

            if (ExceptionToThrow is not null)
            {
                throw ExceptionToThrow;
            }

            return Task.CompletedTask;
        }

        public Task<bool> UpdateAsync(Tenant tenant, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class ConfigurableTenantUniquenessChecker : ITenantUniquenessChecker
    {
        public HashSet<string> OccupiedCompanyNames { get; } = new(StringComparer.Ordinal);

        public HashSet<string> OccupiedCompleteVatNumbers { get; } = new(StringComparer.Ordinal);

        public HashSet<string> OccupiedCertifiedEmails { get; } = new(StringComparer.Ordinal);

        public bool ThrowWhenCancelled { get; set; }

        public Task<bool> IsCompanyNameUniqueAsync(
            string normalizedCompanyName,
            CancellationToken cancellationToken = default)
        {
            ThrowIfCancelled(cancellationToken);
            return Task.FromResult(!OccupiedCompanyNames.Contains(normalizedCompanyName));
        }

        public Task<bool> IsCompleteVatNumberUniqueAsync(
            string normalizedCompleteVatNumber,
            CancellationToken cancellationToken = default)
        {
            ThrowIfCancelled(cancellationToken);
            return Task.FromResult(!OccupiedCompleteVatNumbers.Contains(normalizedCompleteVatNumber));
        }

        public Task<bool> IsCertifiedEmailUniqueAsync(
            string normalizedCertifiedEmail,
            CancellationToken cancellationToken = default)
        {
            ThrowIfCancelled(cancellationToken);
            return Task.FromResult(!OccupiedCertifiedEmails.Contains(normalizedCertifiedEmail));
        }

        private void ThrowIfCancelled(CancellationToken cancellationToken)
        {
            if (ThrowWhenCancelled && cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException(cancellationToken);
            }
        }
    }
}
