using PSAAS.AdminPortal.Features.Tenants.Services;
using PSAAS.AdminPortal.Features.Tenants.ViewModels;
using PSAAS.Application.Abstractions.Tenants;
using PSAAS.Application.Contracts.Tenants;
using PSAAS.Application.Validators.Tenants;
using Xunit;

namespace PSAAS.AdminPortal.Tests;

public sealed class TenantCreateFormViewModelTests : IDisposable
{
    private readonly FakeTenantCreateService _createService = new();
    private readonly TenantCreateFormViewModel _viewModel;

    public TenantCreateFormViewModelTests()
    {
        _viewModel = new TenantCreateFormViewModel(
            new TenantCreateValidator(new AlwaysUniqueTenantUniquenessChecker()),
            _createService);
    }

    public void Dispose()
    {
        _createService.DelayCompletion?.TrySetResult();
        _viewModel.Dispose();
    }

    private void FillValidValues()
    {
        _viewModel.CompanyName = "Acme S.r.l.";
        _viewModel.VatNumber = "IT12345678901";
        _viewModel.CertifiedEmail = "pec@example.test";
        _viewModel.Street = "Via Roma 1";
        _viewModel.City = "Roma";
        _viewModel.PostalCode = "00100";
        _viewModel.Country = "Italia";
    }

    [Fact]
    public async Task SubmitAsync_ReturnsFalse_AndMarksRequiredErrors_WhenFieldsAreEmpty()
    {
        // The form initializes the country to Italia, so it is cleared
        // explicitly to cover the required error path for every field.
        _viewModel.Country = null;

        var submitted = await _viewModel.SubmitAsync();

        Assert.False(submitted);
        Assert.Equal(TenantFieldValidationErrorCodes.Required, _viewModel.CompanyNameErrorCode);
        Assert.Equal(TenantFieldValidationErrorCodes.Required, _viewModel.VatNumberErrorCode);
        Assert.Equal(TenantFieldValidationErrorCodes.Required, _viewModel.CertifiedEmailErrorCode);
        Assert.Equal(TenantFieldValidationErrorCodes.Required, _viewModel.StreetErrorCode);
        Assert.Equal(TenantFieldValidationErrorCodes.Required, _viewModel.CityErrorCode);
        Assert.Equal(TenantFieldValidationErrorCodes.Required, _viewModel.PostalCodeErrorCode);
        Assert.Equal(TenantFieldValidationErrorCodes.Required, _viewModel.CountryErrorCode);
        Assert.Null(_viewModel.SaveErrorCode);
        Assert.False(_viewModel.IsSaving);
        Assert.Empty(_createService.Requests);
    }

    [Fact]
    public async Task SubmitAsync_SubmitsRequest_AndReturnsTrue_WhenFieldsAreValid()
    {
        FillValidValues();

        var submitted = await _viewModel.SubmitAsync();

        Assert.True(submitted);
        Assert.Null(_viewModel.SaveErrorCode);
        Assert.False(_viewModel.IsSaving);

        var request = Assert.Single(_createService.Requests);
        Assert.Equal("Acme S.r.l.", request.CompanyName);
        Assert.Equal("IT12345678901", request.CompleteVatNumber);
        Assert.Equal("pec@example.test", request.CertifiedEmail);
        Assert.Equal("Via Roma 1", request.Street);
        Assert.Equal("Roma", request.City);
        Assert.Equal("00100", request.PostalCode);
        Assert.Equal("Italia", request.Country);
    }

    [Fact]
    public async Task SubmitAsync_MapsServiceFieldErrors_AndReturnsFalse()
    {
        FillValidValues();
        _createService.ResultToReturn = TenantCreateResult.Invalid(
            certifiedEmailErrorCode: TenantFieldValidationErrorCodes.NotUnique);

        var submitted = await _viewModel.SubmitAsync();

        Assert.False(submitted);
        Assert.Equal(TenantFieldValidationErrorCodes.NotUnique, _viewModel.CertifiedEmailErrorCode);
        Assert.Null(_viewModel.CompanyNameErrorCode);
        Assert.Null(_viewModel.SaveErrorCode);
        Assert.False(_viewModel.IsSaving);
    }

    [Fact]
    public async Task SubmitAsync_MarksCompanyNameMaxLengthError_AndDoesNotCallService()
    {
        FillValidValues();
        _viewModel.CompanyName = "123456789012345678901";

        var submitted = await _viewModel.SubmitAsync();

        Assert.False(submitted);
        Assert.Equal(TenantFieldValidationErrorCodes.MaxLengthExceeded, _viewModel.CompanyNameErrorCode);
        Assert.Null(_viewModel.CertifiedEmailErrorCode);
        Assert.Empty(_createService.Requests);
    }

    [Fact]
    public async Task SubmitAsync_MarksCertifiedEmailInvalidFormatError_AndDoesNotCallService()
    {
        FillValidValues();
        _viewModel.CertifiedEmail = "not-an-email";

        var submitted = await _viewModel.SubmitAsync();

        Assert.False(submitted);
        Assert.Equal(TenantFieldValidationErrorCodes.InvalidFormat, _viewModel.CertifiedEmailErrorCode);
        Assert.Null(_viewModel.CompanyNameErrorCode);
        Assert.Empty(_createService.Requests);
    }

    [Fact]
    public async Task SubmitAsync_ShowsSaveError_AndReturnsFalse_WhenServiceFails()
    {
        FillValidValues();
        _createService.ResultToReturn = TenantCreateResult.Failed(TenantCreateErrorCodes.SaveFailed);

        var submitted = await _viewModel.SubmitAsync();

        Assert.False(submitted);
        Assert.Equal(TenantCreateErrorCodes.SaveFailed, _viewModel.SaveErrorCode);
        Assert.False(_viewModel.IsSaving);
    }

    [Fact]
    public async Task SubmitAsync_ShowsSaveError_AndReturnsFalse_WhenServiceThrows()
    {
        FillValidValues();
        _createService.ExceptionToThrow = new InvalidOperationException("unexpected failure");

        var submitted = await _viewModel.SubmitAsync();

        Assert.False(submitted);
        Assert.Equal(TenantCreateErrorCodes.SaveFailed, _viewModel.SaveErrorCode);
        Assert.False(_viewModel.IsSaving);
    }

    [Fact]
    public async Task SubmitAsync_IgnoresConcurrentSubmit_WhileSaving()
    {
        FillValidValues();
        _createService.DelayCompletion = new TaskCompletionSource();

        var firstSubmit = _viewModel.SubmitAsync();
        var secondSubmit = _viewModel.SubmitAsync();

        Assert.True(secondSubmit.IsCompleted);
        Assert.False(await secondSubmit);
        Assert.Single(_createService.Requests);

        _createService.DelayCompletion.SetResult();

        Assert.True(await firstSubmit);
        Assert.False(_viewModel.IsSaving);
    }

    private sealed class FakeTenantCreateService : ITenantCreateService
    {
        public List<TenantCreateRequest> Requests { get; } = [];

        public TenantCreateResult? ResultToReturn { get; set; }

        public Exception? ExceptionToThrow { get; set; }

        public TaskCompletionSource? DelayCompletion { get; set; }

        public async Task<TenantCreateResult> CreateTenantAsync(
            TenantCreateRequest request,
            CancellationToken cancellationToken = default)
        {
            Requests.Add(request);

            if (DelayCompletion is not null)
            {
                await DelayCompletion.Task.ConfigureAwait(false);
            }

            if (ExceptionToThrow is not null)
            {
                throw ExceptionToThrow;
            }

            return ResultToReturn ?? TenantCreateResult.Created();
        }
    }

    private sealed class AlwaysUniqueTenantUniquenessChecker : ITenantUniquenessChecker
    {
        public Task<bool> IsCompanyNameUniqueAsync(
            string normalizedCompanyName,
            CancellationToken cancellationToken = default) => Task.FromResult(true);

        public Task<bool> IsCompleteVatNumberUniqueAsync(
            string normalizedCompleteVatNumber,
            CancellationToken cancellationToken = default) => Task.FromResult(true);

        public Task<bool> IsCertifiedEmailUniqueAsync(
            string normalizedCertifiedEmail,
            CancellationToken cancellationToken = default) => Task.FromResult(true);
    }
}
