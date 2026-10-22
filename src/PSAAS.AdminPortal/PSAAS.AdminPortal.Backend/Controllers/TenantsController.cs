using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using NLog;
using PSAAS.AdminPortal.Backend.Contracts;
using PSAAS.Application.Abstractions.Tenants;
using PSAAS.Application.Abstractions.Users;
using PSAAS.Application.Contracts;
using PSAAS.Application.Contracts.Tenants;
using PSAAS.Application.Services.Tenants;
using PSAAS.Application.Validators.Tenants;

namespace PSAAS.AdminPortal.Backend.Controllers;

[ApiController]
[Route("api/tenants")]
public sealed class TenantsController(ITenantReadRepository tenants, IUserDirectory users) : ControllerBase
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    /// <summary>UC1 + UC21 (mock 1c): search, filters (status and planId repeatable), sort, page.</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<TenantListItem>>> GetPage(
        [FromQuery] TenantListParameters parameters,
        [FromServices] TenantListService list,
        [FromQuery, Range(1, int.MaxValue)] int page = 1,
        [FromQuery, Range(1, 100)] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = parameters.ToQuery(page, pageSize);
        if (TenantListService.InvalidParameter(query) is { } field) return InvalidParameter(field);
        return await list.GetListAsync(query, cancellationToken);
    }

    /// <summary>UC21-R1: the numbers of the tabs.</summary>
    [HttpGet("counts")]
    public Task<TenantCounts> GetCounts([FromServices] TenantListService list, CancellationToken cancellationToken) =>
        list.GetCountsAsync(cancellationToken);

    /// <summary>UC21-R5: the filtered tenants (or only ids=…) as CSV.</summary>
    [HttpGet("export")]
    public async Task<IActionResult> Export([FromQuery] TenantListParameters parameters, [FromServices] TenantListService list, CancellationToken cancellationToken)
    {
        var query = parameters.ToQuery(1, TenantListService.MaxExportRows);
        if (TenantListService.InvalidParameter(query) is { } field) return InvalidParameter(field);
        var csv = await list.ExportCsvAsync(query, cancellationToken);
        // BOM: Excel opens the accented names correctly.
        return File(System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(csv)).ToArray(), "text/csv; charset=utf-8", "clienti.csv");
    }

    private ActionResult InvalidParameter(string field) =>
        ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]> { [field] = [TenantFieldValidationErrorCodes.InvalidValue] }));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TenantDetailDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var tenant = await tenants.GetByIdAsync(id, cancellationToken);
        if (tenant is null)
        {
            Logger.Info("Tenant {TenantId} not found", id);
            return NotFound();
        }

        var structure = await tenants.GetProfilesAndProductiveUnitsAsync(id.ToString(), cancellationToken);
        var userCounts = await CountUsersAsync(structure.Profiles.Select(profile => profile.Id).ToArray(), cancellationToken);
        Logger.Info(
            "Returned tenant {TenantId} with {ProfileCount} profiles and {ProductiveUnitCount} productive units",
            id,
            structure.Profiles.Count,
            structure.ProductiveUnits.Count);
        return new TenantDetailDto(
            tenant.Id,
            tenant.CompanyName,
            tenant.VatNumber,
            tenant.CompleteVatNumber,
            tenant.CertifiedEmail,
            AddressDto.From(tenant.Address),
            structure.Profiles
                .Select(profile => new TenantProfileDto(
                    profile.Id, profile.Name, profile.Description, userCounts?.GetValueOrDefault(profile.Id)))
                .ToArray(),
            structure.ProductiveUnits,
            tenant.Code,
            TenantLifecycleDto.From(tenant.Lifecycle),
            tenant.Commercial.SdiCode,
            tenant.Commercial.AccountManager);
    }

    // UC13: status change along the allowed transitions; the response is the updated detail.
    [HttpPost("{id:guid}/status")]
    public async Task<ActionResult<TenantDetailDto>> ChangeStatus(
        Guid id,
        TenantStatusChangeRequest request,
        [FromServices] TenantLifecycleService lifecycle,
        CancellationToken cancellationToken)
    {
        var result = await lifecycle.ChangeStatusAsync(id, request, cancellationToken);
        return await EditResultAsync(id, result, cancellationToken);
    }

    // UC14: only a draft (Onboarding) can be deleted; any other tenant is ceased instead (UC13).
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DiscardDraft(Guid id, [FromServices] TenantLifecycleService lifecycle, CancellationToken cancellationToken)
    {
        return await lifecycle.DiscardDraftAsync(id, cancellationToken) switch
        {
            DiscardResult.Discarded => NoContent(),
            DiscardResult.NotFound => NotFound(),
            _ => Conflict(),
        };
    }

    // The detail stays readable without Keycloak: only the counts are missing.
    private async Task<IReadOnlyDictionary<string, int>?> CountUsersAsync(string[] profileIds, CancellationToken cancellationToken)
    {
        if (profileIds.Length == 0)
        {
            return new Dictionary<string, int>();
        }

        try
        {
            return await users.CountByProfilesAsync(profileIds, cancellationToken);
        }
        catch (UserDirectoryUnavailableException exception)
        {
            Logger.Warn(exception, "Tenant detail returned without user counts");
            return null;
        }
    }

    // UC4.
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<TenantDetailDto>> UpdateRegistry(
        Guid id,
        TenantRegistryUpdateRequest request,
        [FromServices] TenantRegistryUpdateService updateService,
        CancellationToken cancellationToken)
    {
        var result = await updateService.UpdateAsync(id, request, cancellationToken);
        return await EditResultAsync(id, result, cancellationToken);
    }

    // UC5.
    [HttpPut("{id:guid}/organization")]
    public async Task<ActionResult<TenantDetailDto>> SaveOrganization(
        Guid id,
        TenantOrganizationRequest request,
        [FromServices] TenantOrganizationService organizationService,
        CancellationToken cancellationToken)
    {
        var result = await organizationService.SaveAsync(id, request, cancellationToken);
        return await EditResultAsync(id, result, cancellationToken);
    }

    private async Task<ActionResult<TenantDetailDto>> EditResultAsync(Guid id, TenantEditResult result, CancellationToken cancellationToken)
    {
        if (!result.Found)
        {
            return NotFound();
        }

        if (!result.Succeeded)
        {
            // Field details are logged by the service.
            return ValidationProblem(new ValidationProblemDetails(
                result.Errors.ToDictionary(error => error.Key, error => new[] { error.Value })));
        }

        return await GetById(id, cancellationToken);
    }

    // UC11: same rules and codes as saving (UC3/UC4), one field at a time, so the form can check on blur.
    [HttpGet("field-validation")]
    public async Task<ActionResult<TenantFieldValidationDto>> ValidateField(
        [FromQuery, Required] string field,
        [FromQuery] string? value,
        [FromQuery] Guid? excludeTenantId,
        [FromServices] TenantCreateValidator validator,
        CancellationToken cancellationToken)
    {
        var result = field switch
        {
            "companyName" => await validator.ValidateCompanyNameAsync(value, excludeTenantId, cancellationToken),
            "completeVatNumber" => await validator.ValidateCompleteVatNumberAsync(value, excludeTenantId, cancellationToken),
            "certifiedEmail" => await validator.ValidateCertifiedEmailAsync(value, excludeTenantId, cancellationToken),
            _ => null
        };
        if (result is null)
        {
            Logger.Info("Field validation requested for unsupported field {Field}", field);
            return ValidationProblem(new ValidationProblemDetails(
                new Dictionary<string, string[]> { ["field"] = [TenantFieldValidationErrorCodes.InvalidValue] }));
        }

        // Debug: the frontend calls this every time one of these fields loses focus.
        Logger.Debug(
            "Field {Field} validated with result {ErrorCode} (excluded tenant {ExcludedTenantId})",
            field,
            result.ErrorCode ?? "Valid",
            excludeTenantId);
        var conflicting = result.ErrorCode == TenantFieldValidationErrorCodes.NotUnique
            ? await tenants.FindByUniqueValueAsync(field, result.NormalizedValue, excludeTenantId, cancellationToken)
            : null;
        return new TenantFieldValidationDto(result.ErrorCode, conflicting);
    }

    [HttpPost]
    public async Task<ActionResult<TenantDetailDto>> Create(
        TenantCreateRequest request,
        [FromServices] TenantCreateService createService,
        CancellationToken cancellationToken)
    {
        var result = await createService.CreateAsync(request, cancellationToken);
        if (!result.Succeeded)
        {
            // Field details are logged by TenantCreateService.
            Logger.Info("Tenant creation answered 400 with {InvalidFieldCount} invalid fields", result.Errors.Count);
            return ValidationProblem(new ValidationProblemDetails(
                result.Errors.ToDictionary(error => error.Key, error => new[] { error.Value })));
        }

        var id = result.TenantId!.Value;
        return CreatedAtAction(nameof(GetById), new { id }, (await GetById(id, cancellationToken)).Value);
    }
}
