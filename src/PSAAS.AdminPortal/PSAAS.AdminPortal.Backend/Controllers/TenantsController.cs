using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using NLog;
using PSAAS.AdminPortal.Backend.Contracts;
using PSAAS.Application.Abstractions.Tenants;
using PSAAS.Application.Contracts;
using PSAAS.Application.Contracts.Tenants;
using PSAAS.Application.Services.Tenants;
using PSAAS.Application.Validators.Tenants;

namespace PSAAS.AdminPortal.Backend.Controllers;

[ApiController]
[Route("api/tenants")]
public sealed class TenantsController(ITenantReadRepository tenants) : ControllerBase
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    [HttpGet]
    public async Task<PagedResult<TenantSummaryDto>> GetPage(
        [FromQuery] string? search,
        [FromQuery, Range(1, int.MaxValue)] int page = 1,
        [FromQuery, Range(1, 100)] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await tenants.GetPageAsync(search, page, pageSize, cancellationToken);
        Logger.Info(
            "Listed {TenantCount} of {TotalCount} tenants (page {Page}, size {PageSize}, search {Search})",
            result.Items.Count,
            result.TotalCount,
            page,
            pageSize,
            search);
        return new PagedResult<TenantSummaryDto>(
            result.Items.Select(TenantSummaryDto.From).ToArray(),
            result.TotalCount,
            result.Page,
            result.PageSize);
    }

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
            structure.Profiles,
            structure.ProductiveUnits);
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
        return new TenantFieldValidationDto(result.ErrorCode);
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
