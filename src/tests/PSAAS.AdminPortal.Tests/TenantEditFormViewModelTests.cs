using PSAAS.AdminPortal.Features.Tenants.Services;
using PSAAS.AdminPortal.Features.Tenants.ViewModels;
using PSAAS.Application.Abstractions.Tenants;
using PSAAS.Application.Contracts.Tenants;
using PSAAS.Application.Validators.Tenants;
using Xunit;

namespace PSAAS.AdminPortal.Tests;

public sealed class TenantEditFormViewModelTests : IDisposable
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly FakeTenantEditService _editService = new();
    private readonly TrackingTenantUniquenessChecker _uniquenessChecker = new();
    private readonly TenantEditFormViewModel _viewModel;

    public TenantEditFormViewModelTests()
    {
        _viewModel = new TenantEditFormViewModel(
            new TenantCreateValidator(_uniquenessChecker),
            _editService)
        {
            TenantId = _tenantId
        };
    }

    public void Dispose()
    {
        _viewModel.Dispose();
    }

    [Fact]
    public async Task InitializeAsync_LoadsTenantValues()
    {
        _editService.ReadModelToReturn = ValidReadModel();

        await _viewModel.InitializeAsync();

        Assert.False(_viewModel.IsLoading);
        Assert.Null(_viewModel.LoadErrorCode);
        Assert.Equal("Acme S.r.l.", _viewModel.CompanyName);
        Assert.Equal("IT12345678901", _viewModel.VatNumber);
        Assert.Equal("pec@example.test", _viewModel.CertifiedEmail);
        Assert.Equal("Via Roma 1", _viewModel.Street);
        Assert.Equal("Roma", _viewModel.City);
        Assert.Equal("00100", _viewModel.PostalCode);
        Assert.Equal("Italia", _viewModel.Country);
    }

    [Fact]
    public async Task InitializeAsync_SetsNotFound_WhenTenantDoesNotExist()
    {
        _editService.ReadModelToReturn = null;

        await _viewModel.InitializeAsync();

        Assert.True(_viewModel.IsNotFound);
        Assert.Equal(TenantEditErrorCodes.NotFound, _viewModel.LoadErrorCode);
        Assert.False(_viewModel.IsLoading);
    }

    [Fact]
    public async Task SubmitAsync_UpdatesTenant_AndUsesTenantIdForUniquenessExclusion()
    {
        _editService.ReadModelToReturn = ValidReadModel();
        await _viewModel.InitializeAsync();

        var submitted = await _viewModel.SubmitAsync();

        Assert.True(submitted);
        Assert.Null(_viewModel.SaveErrorCode);

        var request = Assert.Single(_editService.UpdateRequests);
        Assert.Equal(_tenantId, request.Id);
        Assert.Equal("Acme S.r.l.", request.CompanyName);
        Assert.Equal("IT12345678901", request.CompleteVatNumber);
        Assert.Equal("pec@example.test", request.CertifiedEmail);

        Assert.Equal(_tenantId, _uniquenessChecker.LastExcludedCompanyNameTenantId);
        Assert.Equal(_tenantId, _uniquenessChecker.LastExcludedCompleteVatNumberTenantId);
        Assert.Equal(_tenantId, _uniquenessChecker.LastExcludedCertifiedEmailTenantId);
    }

    [Fact]
    public async Task SubmitAsync_MapsServiceFieldErrors_AndReturnsFalse()
    {
        _editService.ReadModelToReturn = ValidReadModel();
        _editService.ResultToReturn = TenantEditResult.Invalid(
            completeVatNumberErrorCode: TenantFieldValidationErrorCodes.NotUnique);
        await _viewModel.InitializeAsync();

        var submitted = await _viewModel.SubmitAsync();

        Assert.False(submitted);
        Assert.Equal(TenantFieldValidationErrorCodes.NotUnique, _viewModel.VatNumberErrorCode);
        Assert.Null(_viewModel.SaveErrorCode);
    }

    [Fact]
    public async Task SubmitAsync_ShowsNotFoundSaveError_WhenServiceReturnsNotFound()
    {
        _editService.ReadModelToReturn = ValidReadModel();
        _editService.ResultToReturn = TenantEditResult.Failed(TenantEditErrorCodes.NotFound);
        await _viewModel.InitializeAsync();

        var submitted = await _viewModel.SubmitAsync();

        Assert.False(submitted);
        Assert.Equal(TenantEditErrorCodes.NotFound, _viewModel.SaveErrorCode);
    }

    private TenantEditReadModel ValidReadModel() =>
        new(
            _tenantId,
            "Acme S.r.l.",
            "IT12345678901",
            "pec@example.test",
            "Via Roma 1",
            "Roma",
            "00100",
            "Italia");

    private sealed class FakeTenantEditService : ITenantEditService
    {
        public TenantEditReadModel? ReadModelToReturn { get; set; }

        public List<TenantEditRequest> UpdateRequests { get; } = [];

        public TenantEditResult? ResultToReturn { get; set; }

        public Task<TenantEditReadModel?> GetTenantForEditAsync(Guid tenantId, CancellationToken cancellationToken = default) =>
            Task.FromResult(ReadModelToReturn);

        public Task<TenantEditResult> UpdateTenantAsync(TenantEditRequest request, CancellationToken cancellationToken = default)
        {
            UpdateRequests.Add(request);
            return Task.FromResult(ResultToReturn ?? TenantEditResult.Updated());
        }
    }

    private sealed class TrackingTenantUniquenessChecker : ITenantUniquenessChecker
    {
        public Guid? LastExcludedCompanyNameTenantId { get; private set; }

        public Guid? LastExcludedCompleteVatNumberTenantId { get; private set; }

        public Guid? LastExcludedCertifiedEmailTenantId { get; private set; }

        public Task<bool> IsCompanyNameUniqueAsync(string normalizedCompanyName, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);

        public Task<bool> IsCompanyNameUniqueAsync(string normalizedCompanyName, Guid excludedTenantId, CancellationToken cancellationToken = default)
        {
            LastExcludedCompanyNameTenantId = excludedTenantId;
            return Task.FromResult(true);
        }

        public Task<bool> IsCompleteVatNumberUniqueAsync(string normalizedCompleteVatNumber, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);

        public Task<bool> IsCompleteVatNumberUniqueAsync(string normalizedCompleteVatNumber, Guid excludedTenantId, CancellationToken cancellationToken = default)
        {
            LastExcludedCompleteVatNumberTenantId = excludedTenantId;
            return Task.FromResult(true);
        }

        public Task<bool> IsCertifiedEmailUniqueAsync(string normalizedCertifiedEmail, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);

        public Task<bool> IsCertifiedEmailUniqueAsync(string normalizedCertifiedEmail, Guid excludedTenantId, CancellationToken cancellationToken = default)
        {
            LastExcludedCertifiedEmailTenantId = excludedTenantId;
            return Task.FromResult(true);
        }
    }
}
