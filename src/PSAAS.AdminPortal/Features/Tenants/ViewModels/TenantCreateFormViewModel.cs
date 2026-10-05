using PSAAS.AdminPortal.Features.Tenants.Services;
using PSAAS.Application.Contracts.Tenants;
using PSAAS.Application.Validators.Tenants;
using PSAAS.MVVM;

namespace PSAAS.AdminPortal.Features.Tenants.ViewModels;

public sealed class TenantCreateFormViewModel(
    TenantCreateValidator tenantCreateValidator,
    ITenantCreateService tenantCreateService) : InjectedViewModel
{
    private string? _companyName;
    private string? _vatNumber;
    private string? _certifiedEmail;
    private string? _street;
    private string? _city;
    private string? _postalCode;
    private string? _country = "Italia";

    private string? _companyNameErrorCode;
    private string? _vatNumberErrorCode;
    private string? _certifiedEmailErrorCode;
    private string? _streetErrorCode;
    private string? _cityErrorCode;
    private string? _postalCodeErrorCode;
    private string? _countryErrorCode;

    private bool _isSaving;
    private string? _saveErrorCode;

    private CancellationTokenSource? _companyNameValidationCancellation;
    private CancellationTokenSource? _vatNumberValidationCancellation;
    private CancellationTokenSource? _certifiedEmailValidationCancellation;

    private long _companyNameValidationVersion;
    private long _vatNumberValidationVersion;
    private long _certifiedEmailValidationVersion;

    public string? CompanyName
    {
        get => _companyName;
        set
        {
            if (SetProperty(ref _companyName, value))
            {
                CancelCompanyNameValidation();
            }
        }
    }

    public string? VatNumber
    {
        get => _vatNumber;
        set
        {
            if (SetProperty(ref _vatNumber, value))
            {
                CancelVatNumberValidation();
            }
        }
    }

    public string? CertifiedEmail
    {
        get => _certifiedEmail;
        set
        {
            if (SetProperty(ref _certifiedEmail, value))
            {
                CancelCertifiedEmailValidation();
            }
        }
    }

    public string? Street
    {
        get => _street;
        set
        {
            if (SetProperty(ref _street, value) && StreetErrorCode is not null)
            {
                ValidateStreet();
            }
        }
    }

    public string? City
    {
        get => _city;
        set
        {
            if (SetProperty(ref _city, value) && CityErrorCode is not null)
            {
                ValidateCity();
            }
        }
    }

    public string? PostalCode
    {
        get => _postalCode;
        set
        {
            if (SetProperty(ref _postalCode, value) && PostalCodeErrorCode is not null)
            {
                ValidatePostalCode();
            }
        }
    }

    public string? Country
    {
        get => _country;
        set
        {
            if (SetProperty(ref _country, value) && CountryErrorCode is not null)
            {
                ValidateCountry();
            }
        }
    }

    public string? CompanyNameErrorCode
    {
        get => _companyNameErrorCode;
        private set => SetProperty(ref _companyNameErrorCode, value);
    }

    public string? VatNumberErrorCode
    {
        get => _vatNumberErrorCode;
        private set => SetProperty(ref _vatNumberErrorCode, value);
    }

    public string? CertifiedEmailErrorCode
    {
        get => _certifiedEmailErrorCode;
        private set => SetProperty(ref _certifiedEmailErrorCode, value);
    }

    public string? StreetErrorCode
    {
        get => _streetErrorCode;
        private set => SetProperty(ref _streetErrorCode, value);
    }

    public string? CityErrorCode
    {
        get => _cityErrorCode;
        private set => SetProperty(ref _cityErrorCode, value);
    }

    public string? PostalCodeErrorCode
    {
        get => _postalCodeErrorCode;
        private set => SetProperty(ref _postalCodeErrorCode, value);
    }

    public string? CountryErrorCode
    {
        get => _countryErrorCode;
        private set => SetProperty(ref _countryErrorCode, value);
    }

    /// <summary>
    /// Gets a value indicating whether a save is currently in progress,
    /// used to prevent concurrent submits.
    /// </summary>
    public bool IsSaving
    {
        get => _isSaving;
        private set => SetProperty(ref _isSaving, value);
    }

    /// <summary>
    /// Gets the error code of a failed save that is not tied to a single field,
    /// or null when the last submit did not fail generically.
    /// </summary>
    public string? SaveErrorCode
    {
        get => _saveErrorCode;
        private set => SetProperty(ref _saveErrorCode, value);
    }

    /// <summary>
    /// Validates every field, then submits the tenant to the creation service.
    /// Returns true when the tenant was created; false when validation failed,
    /// the service reported errors or the submit was skipped because another
    /// one is already in progress.
    /// </summary>
    public async Task<bool> SubmitAsync()
    {
        if (IsSaving)
        {
            return false;
        }

        IsSaving = true;
        SaveErrorCode = null;

        try
        {
            await ValidateCompanyNameAsync().ConfigureAwait(false);
            await ValidateVatNumberAsync().ConfigureAwait(false);
            await ValidateCertifiedEmailAsync().ConfigureAwait(false);
            ValidateStreet();
            ValidateCity();
            ValidatePostalCode();
            ValidateCountry();

            if (HasFieldErrors)
            {
                return false;
            }

            // The form has a single VAT field that carries the complete VAT
            // number; the service derives the 11-digit VatNumber from it.
            var request = new TenantCreateRequest(
                CompanyName,
                VatNumber,
                CertifiedEmail,
                Street,
                City,
                PostalCode,
                Country);

            var result = await tenantCreateService
                .CreateTenantAsync(request, DisposalToken)
                .ConfigureAwait(false);

            if (!result.Succeeded)
            {
                CompanyNameErrorCode = result.CompanyNameErrorCode;
                VatNumberErrorCode = result.CompleteVatNumberErrorCode;
                CertifiedEmailErrorCode = result.CertifiedEmailErrorCode;
                StreetErrorCode = result.StreetErrorCode;
                CityErrorCode = result.CityErrorCode;
                PostalCodeErrorCode = result.PostalCodeErrorCode;
                CountryErrorCode = result.CountryErrorCode;
                SaveErrorCode = result.ErrorCode;
                return false;
            }

            return true;
        }
        catch (OperationCanceledException) when (DisposalToken.IsCancellationRequested)
        {
            return false;
        }
        catch (Exception)
        {
            SaveErrorCode = TenantCreateErrorCodes.SaveFailed;
            return false;
        }
        finally
        {
            IsSaving = false;
        }
    }

    private bool HasFieldErrors =>
        CompanyNameErrorCode is not null
        || VatNumberErrorCode is not null
        || CertifiedEmailErrorCode is not null
        || StreetErrorCode is not null
        || CityErrorCode is not null
        || PostalCodeErrorCode is not null
        || CountryErrorCode is not null;

    public async Task ValidateCompanyNameAsync()
    {
        var version = Interlocked.Read(ref _companyNameValidationVersion);
        using var validationCancellation = CreateCompanyNameValidationCancellation();
        await ValidateCompanyNameVersionAsync(version, validationCancellation.Token).ConfigureAwait(false);
    }

    public async Task ValidateVatNumberAsync()
    {
        var version = Interlocked.Read(ref _vatNumberValidationVersion);
        using var validationCancellation = CreateVatNumberValidationCancellation();
        await ValidateVatNumberVersionAsync(version, validationCancellation.Token).ConfigureAwait(false);
    }

    public async Task ValidateCertifiedEmailAsync()
    {
        var version = Interlocked.Read(ref _certifiedEmailValidationVersion);
        using var validationCancellation = CreateCertifiedEmailValidationCancellation();
        await ValidateCertifiedEmailVersionAsync(version, validationCancellation.Token).ConfigureAwait(false);
    }

    public void ValidateStreet() => StreetErrorCode = ToRequiredErrorCode(Street);

    public void ValidateCity() => CityErrorCode = ToRequiredErrorCode(City);

    public void ValidatePostalCode() => PostalCodeErrorCode = ToRequiredErrorCode(PostalCode);

    public void ValidateCountry() => CountryErrorCode = ToRequiredErrorCode(Country);

    protected override void DisposeManagedResources()
    {
        DisposeValidationCancellation(ref _companyNameValidationCancellation);
        DisposeValidationCancellation(ref _vatNumberValidationCancellation);
        DisposeValidationCancellation(ref _certifiedEmailValidationCancellation);
        base.DisposeManagedResources();
    }

    private async Task ValidateCompanyNameVersionAsync(long version, CancellationToken cancellationToken)
    {
        try
        {
            var result = await tenantCreateValidator.ValidateCompanyNameAsync(CompanyName, cancellationToken).ConfigureAwait(false);
            if (version == Interlocked.Read(ref _companyNameValidationVersion) && !cancellationToken.IsCancellationRequested)
            {
                CompanyNameErrorCode = ToErrorCode(result);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
    }

    private async Task ValidateVatNumberVersionAsync(long version, CancellationToken cancellationToken)
    {
        try
        {
            var result = await tenantCreateValidator.ValidateCompleteVatNumberAsync(VatNumber, cancellationToken).ConfigureAwait(false);
            if (version == Interlocked.Read(ref _vatNumberValidationVersion) && !cancellationToken.IsCancellationRequested)
            {
                VatNumberErrorCode = ToErrorCode(result);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
    }

    private async Task ValidateCertifiedEmailVersionAsync(long version, CancellationToken cancellationToken)
    {
        try
        {
            var result = await tenantCreateValidator.ValidateCertifiedEmailAsync(CertifiedEmail, cancellationToken).ConfigureAwait(false);
            if (version == Interlocked.Read(ref _certifiedEmailValidationVersion) && !cancellationToken.IsCancellationRequested)
            {
                CertifiedEmailErrorCode = ToErrorCode(result);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
    }

    private CancellationTokenSource CreateCompanyNameValidationCancellation()
    {
        DisposeValidationCancellation(ref _companyNameValidationCancellation);
        _companyNameValidationCancellation = CancellationTokenSource.CreateLinkedTokenSource(DisposalToken);
        return _companyNameValidationCancellation;
    }

    private CancellationTokenSource CreateVatNumberValidationCancellation()
    {
        DisposeValidationCancellation(ref _vatNumberValidationCancellation);
        _vatNumberValidationCancellation = CancellationTokenSource.CreateLinkedTokenSource(DisposalToken);
        return _vatNumberValidationCancellation;
    }

    private CancellationTokenSource CreateCertifiedEmailValidationCancellation()
    {
        DisposeValidationCancellation(ref _certifiedEmailValidationCancellation);
        _certifiedEmailValidationCancellation = CancellationTokenSource.CreateLinkedTokenSource(DisposalToken);
        return _certifiedEmailValidationCancellation;
    }

    private void CancelCompanyNameValidation()
    {
        Interlocked.Increment(ref _companyNameValidationVersion);
        CancelValidation(ref _companyNameValidationCancellation);
    }

    private void CancelVatNumberValidation()
    {
        Interlocked.Increment(ref _vatNumberValidationVersion);
        CancelValidation(ref _vatNumberValidationCancellation);
    }

    private void CancelCertifiedEmailValidation()
    {
        Interlocked.Increment(ref _certifiedEmailValidationVersion);
        CancelValidation(ref _certifiedEmailValidationCancellation);
    }

    private static string? ToErrorCode(TenantFieldValidationResult result) =>
        result.IsValid ? null : result.ErrorCode;

    private static string? ToRequiredErrorCode(string? value) =>
        string.IsNullOrWhiteSpace(value) ? TenantFieldValidationErrorCodes.Required : null;

    private static void CancelValidation(ref CancellationTokenSource? cancellationTokenSource)
    {
        try
        {
            cancellationTokenSource?.Cancel();
        }
        catch (ObjectDisposedException)
        {
        }
    }

    private static void DisposeValidationCancellation(ref CancellationTokenSource? cancellationTokenSource)
    {
        var current = cancellationTokenSource;
        cancellationTokenSource = null;
        if (current is null)
        {
            return;
        }

        try
        {
            current.Cancel();
        }
        catch (ObjectDisposedException)
        {
        }

        current.Dispose();
    }
}
